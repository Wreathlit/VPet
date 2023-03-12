using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace VPet_Simulator.Core
{
    public class AnimationController
    {
        public static readonly Lazy<AnimationController> _instance = new Lazy<AnimationController>(() => new AnimationController());

        public static AnimationController Instance
        {
            get { return _instance.Value; }
        }

        protected IGraphNew graph;

        protected Random random;

        private List<AnimationInfo> rawAnimations;
        private List<AnimationInfo> animations;

        private Dictionary<string, FrameInfo> framesCache;

        private List<string> existAnimations;


        /// <summary>
        /// 动画控制器，接收逻辑状态，输出动画状态
        /// </summary>
        AnimationController()
        {
            Initialize();
        }

        public void RegistryGraph(IGraphNew graph)
        {
            this.graph = graph;
        }

        //重新初始化控制器，目前数据暂时无法统一初始化
        public void Initialize()
        {
            Dispose();
            //TODO：此处应该读取mod中包含的动画定义数据，暂时先手写代替
            rawAnimations = new List<AnimationInfo>();
            animations = new List<AnimationInfo>();
            framesCache = new Dictionary<string, FrameInfo>();
            OrderList = new ConcurrentQueue<Action>();
            CommandList = new ConcurrentQueue<Action>();
            random = new Random();
            ProcessOrderThread = new Thread(ProcessOrder);
            ProcessOrderThread.Start();
        }

        public void AddAnimation(DirectoryInfo directoryInfo, string animationName)
        {
            if (directoryInfo.Exists && directoryInfo.GetFiles().Length > 0)
            {
                //Console.WriteLine("Add Animation: " + animationName + " => " + directoryInfo.FullName);
                AnimationInfo animation = new AnimationInfo(animationName, "", new Uri(directoryInfo.FullName));
                rawAnimations.Add(animation);

                animation.framesLoop.ForEach(frame => { framesCache.Add(frame.uri.LocalPath, frame); });
            }
        }
        //==============================
        //规则部分，使用现有拼接结构
        //模式分为3种
        static readonly string[] existMode = { @"_Nomal(?=_|$)", "_Happy(?=_|$)", "_Ill(?=_|$)" };
        //Start Loop End分别用_A _B _C标识
        static readonly string[] existSegment = { "_A(?=_|$)", "_B(?=_|$)", "_C(?=_|$)" };
        //随机：总是在最后，以下划线+数字的形式存在

        public void ArrangeAnimation()
        {
            //收集根动画信息，仅接受name和mode的差分
            Dictionary<string, bool> tags = new Dictionary<string, bool>();
            foreach (var a in rawAnimations)
            {
                string name = a.name;
                foreach (var s in existMode)
                {
                    name = Regex.Replace(name, s, "", RegexOptions.IgnoreCase);
                }
                foreach (var s in existSegment)
                {
                    name = Regex.Replace(name, s, "", RegexOptions.IgnoreCase);
                }
                name = Regex.Replace(name, @"_\d+(?=_|$)", "");
                tags[name] = true;
            }
            existAnimations = new List<string>();
            foreach (var kv in tags)
            {
                existAnimations.Add(kv.Key);
                //Console.WriteLine(kv.Key);
            }
            //Console.WriteLine("Arrange Complete");
        }

        private ConcurrentQueue<Action> OrderList;
        private ConcurrentQueue<Action> CommandList;
        private Action currentPlayingCommand;
        private Thread ProcessOrderThread;
        private bool repeatFlag = false;
        private int repeatTimes = 0;
        private void ProcessOrder()
        {
            while (OrderList != null)
            {
                while (!OrderList.IsEmpty)
                {
                    Action a;
                    if (OrderList.TryDequeue(out a))
                    {
                        try
                        {
                            //Console.WriteLine("Invoke Command: " + a.Method.ToString());
                            a.Invoke();
                        }
                        catch (ThreadInterruptedException) { }
                        catch (ThreadAbortException) { return; }
                    }
                }
                if (repeatFlag && repeatTimes != 0 && currentPlayingCommand != null)
                {
                    try
                    {
                        repeatFlag = false;
                        repeatTimes--;
                        currentPlayingCommand.Invoke();
                        Thread.Sleep(1);
                    }
                    catch (ThreadInterruptedException) { }
                    catch (ThreadAbortException) { return; }
                }
                else if (!CommandList.IsEmpty)
                {
                    Action a;
                    if (CommandList.TryDequeue(out a))
                    {
                        try
                        {
                            repeatFlag = false;
                            repeatTimes = 0;
                            currentPlayingCommand = a;
                            a.Invoke();
                            Thread.Sleep(1);
                        }
                        catch (ThreadInterruptedException) { }
                        catch (ThreadAbortException) { return; }
                    }
                }
                else
                {
                    try
                    {
                        Thread.Sleep(1000);
                    }
                    catch (ThreadInterruptedException) { }
                    catch (ThreadAbortException) { return; }
                }
            }
        }

        private void OrderAnimation(string name, string mode, int loopTimes, bool forceExit, Action additionalAction)
        {
            var accurate = rawAnimations.Where(x => x.name.ToLower().Contains(name) && x.name.ToLower().Contains(mode.ToLower())).ToList();
            if (!accurate.Any())
                accurate = rawAnimations.Where(x => x.name.ToLower().Contains(name) && x.name.ToLower().Contains("nomal")).ToList();
            if (!accurate.Any())
                accurate = rawAnimations.Where(x => x.name.ToLower().Contains(name) && x.name.ToLower().Contains("happy")).ToList();
            if (!accurate.Any())
                accurate = rawAnimations.Where(x => x.name.ToLower().Contains(name) && x.name.ToLower().Contains("ill")).ToList();
            if (!accurate.Any())
                accurate = rawAnimations.Where(x => x.name.ToLower().Contains(name)).ToList();
            if (!accurate.Any())
                accurate = rawAnimations.Where(x => x.name.ToLower().Contains("default")).ToList();

            AnimationInfo result;

            if (accurate.Count > 0)
            {
                result = accurate.ElementAt(random.Next(accurate.Count));

                if (forceExit)
                {
                    while (!OrderList.IsEmpty)
                    {
                        OrderList.TryDequeue(out _);
                    }
                    while (!CommandList.IsEmpty)
                    {
                        CommandList.TryDequeue(out _);
                    }
                    ProcessOrderThread.Interrupt();
                }

                GenerateTasks(result).ForEach(x => OrderList.Enqueue(x));

                if (additionalAction != null)
                {
                    CommandList.Enqueue(() =>
                    {
                        additionalAction.Invoke();
                    });
                }

                if (ProcessOrderThread.ThreadState == ThreadState.WaitSleepJoin)
                    ProcessOrderThread.Interrupt();
            }
        }

        private List<Action> GenerateTasks(AnimationInfo animation)
        {
            List<Action> tasks = new List<Action>();

            for (int i = 0; i < animation.framesLoop.Count; i++)
            {
                FrameInfo f = animation.framesLoop[i];
                tasks.Add(() =>
                {
                    //Console.WriteLine(DateTime.Now.Second.ToString("00") + "." + DateTime.Now.Millisecond.ToString("000") + "\t" + f.name + "\tFrame: " + f.index + " Start => time:" + f.frameTime);
                    Task.Run(() => graph.Order(f.stream));
                    Thread.Sleep(f.frameTime);
                    //Console.WriteLine(DateTime.Now.Second.ToString("00") + "." + DateTime.Now.Millisecond.ToString("000") + "\t" + f.name + "\t=============>Frame: " + f.index + " End");
                });
            }
            return tasks;
        }

        public void PlayAnimation(string name, string mode, int loopTimes = 0, bool forceExit = false, Action additionalAction = null)
        {
            OrderAnimation(name, mode, loopTimes, forceExit, additionalAction);
        }
        /// <summary>
        /// 循环当前动画
        /// </summary>
        /// <param name="times">循环次数，-1为无限循环，直到有新动作打断</param>
        public void RepeatCurrentAnimation(int times = 1)
        {
            repeatFlag = true;
            repeatTimes = times;
        }

        public void Dispose()
        {
            graph?.Clear();
            while (OrderList != null && !OrderList.IsEmpty)
            {
                OrderList.TryDequeue(out _);
            }
            while (CommandList != null && !CommandList.IsEmpty)
            {
                CommandList.TryDequeue(out _);
            }

            rawAnimations?.ForEach(x => x.Dispose());
            rawAnimations?.Clear();
            rawAnimations = null;

            animations?.ForEach(x => x.Dispose());
            animations?.Clear();
            animations = null;

            framesCache?.Clear();
            framesCache = null;
        }
    }
}
