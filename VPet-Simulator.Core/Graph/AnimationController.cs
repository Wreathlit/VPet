using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using ThreadState = System.Threading.ThreadState;

namespace VPet_Simulator.Core
{
    public class AnimationController
    {
        public static readonly Lazy<AnimationController> _instance = new Lazy<AnimationController>(() => new AnimationController());

        public static AnimationController Instance
        {
            get { return _instance.Value; }
        }

        protected Dictionary<string, IGraph> graphs;

        protected Random random;

        private List<PNGAnimation> animations;

        private Dictionary<string, PNGFrame> framesCache;

        private List<string> existAnimations;


        /// <summary>
        /// 动画控制器，接收逻辑状态，输出动画状态
        /// </summary>
        AnimationController()
        {
            Initialize();
        }

        public void RegistryGraph(IGraph graph, string name)
        {
            if (graphs == null) throw new Exception("Using AnimationController without Initialize");
            if (graphs.ContainsKey(name)) throw new Exception("Registry duplicate graph name");
            graphs.Add(name, graph);
        }

        protected IGraph GetGraphByName(string name)
        {
            if (graphs == null) throw new Exception("Using AnimationController without Initialize");
            if (!graphs.ContainsKey(name))
            {
                throw new Exception("Using a unregistried graph");
            }
            else
            {
                return graphs[name];
            }
        }

        //重新初始化控制器，目前数据暂时无法统一初始化
        public void Initialize()
        {
            Dispose();
            graphs = new Dictionary<string, IGraph>();
            //TODO：此处应该读取mod中包含的动画定义数据，暂时先手写代替
            animations = new List<PNGAnimation>();
            framesCache = new Dictionary<string, PNGFrame>();
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
                PNGAnimation animation = new PNGAnimation(animationName, "", new Uri(directoryInfo.FullName));
                animations.Add(animation);

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
            foreach (var a in animations)
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
            }
        }

        private ConcurrentQueue<Action> OrderList;
        private ConcurrentQueue<Action> CommandList;
        private Action currentPlayingCommand;
        private Thread ProcessOrderThread;
        private string currentPlaying = "";
        private int currentPlayingRandomIndex = 0;
        private bool repeatFlag = false;
        private int repeatTimes = 0;
        private void ProcessOrder()
        {
            while (OrderList != null)
            {
                while (!OrderList.IsEmpty)
                {
                    Action order;
                    if (OrderList.TryDequeue(out order))
                    {
                        try
                        {
                            order.Invoke();
                        }
                        catch (ThreadInterruptedException) { }
                        catch (ThreadAbortException) { return; }
                    }
                }
                if (repeatFlag && repeatTimes != 0 && currentPlayingCommand != null)
                {
                    try
                    {
                        repeatTimes--;
                        repeatFlag = repeatTimes != 0;
                        currentPlayingCommand.Invoke();
                        Thread.Sleep(1);
                    }
                    catch (ThreadInterruptedException) { }
                    catch (ThreadAbortException) { return; }
                }
                else if (!CommandList.IsEmpty)
                {
                    Action command;
                    if (CommandList.TryDequeue(out command))
                    {
                        try
                        {
                            repeatFlag = false;
                            repeatTimes = 0;
                            currentPlayingCommand = command;
                            command.Invoke();
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

        private void OrderAnimation(string graph, string name, string mode, uint loopTimes, bool forceExit, Action callback)
        {
            var accurate = animations.Where(x => x.name.ToLower().Contains(name) && x.name.ToLower().Contains(mode.ToLower())).ToList();
            if (!accurate.Any())
                accurate = animations.Where(x => x.name.ToLower().Contains(name) && x.name.ToLower().Contains("nomal")).ToList();
            if (!accurate.Any())
                accurate = animations.Where(x => x.name.ToLower().Contains(name) && x.name.ToLower().Contains("happy")).ToList();
            if (!accurate.Any())
                accurate = animations.Where(x => x.name.ToLower().Contains(name) && x.name.ToLower().Contains("ill")).ToList();
            if (!accurate.Any())
                accurate = animations.Where(x => x.name.ToLower().Contains(name)).ToList();
            if (!accurate.Any())
                accurate = animations.Where(x => x.name.ToLower().Contains("default")).ToList();

            PNGAnimation result;

            if (accurate.Count > 0)
            {
                int index = 0;
                //设计要求：同名动画循环时随机，但不同名动画连接时，优先取上一个动画的随机结果
                bool isFamilyAnimation = AnimationControllerHelper.IsAnimationFamily(name, currentPlaying);
                if (isFamilyAnimation)
                {
                    index = currentPlayingRandomIndex;
                }
                else
                {
                    index = random.Next(0, accurate.Count);
                }

                while (accurate.Count <= index)
                {
                    index--;
                }

                result = accurate.ElementAt(index);
                currentPlaying = name;
                currentPlayingRandomIndex = index;

                if (forceExit)
                {
                    ClearCommand();
                    ClearOrder();
                    ProcessOrderThread.Interrupt();
                }

                if (loopTimes >= 0)
                {
                    for (int i = 0; i <= loopTimes; i++)
                    {
                        GenerateTasks(graph, result).ForEach(x => OrderList.Enqueue(x));
                    }
                }

                if (callback != null)
                {
                    CommandList.Enqueue(() =>
                    {
                        callback.Invoke();
                    });
                }

                if (ProcessOrderThread.ThreadState == ThreadState.WaitSleepJoin)
                    ProcessOrderThread.Interrupt();
            }
        }

        private List<Action> GenerateTasks(string graph, PNGAnimation animation)
        {
            List<Action> tasks = new List<Action>();

            for (int i = 0; i < animation.framesLoop.Count; i++)
            {
                PNGFrame f = animation.framesLoop[i];
                //预先生成Texture


                tasks.Add(() =>
                {
                    //DateTime t = DateTime.Now;
                    if (!f.isCached)
                    {
                        f.GenerateTexture(GetGraphByName(graph).GetGraphicsDevice());
                    }
                    GetGraphByName(graph).OrderTexture(f.texture, true);
                    //Debug.WriteLine("cost time: " + (DateTime.Now - t).Milliseconds.ToString("000") + " ms");
                    Thread.Sleep(f.frameTime);
                });
            }
            return tasks;
        }

        private void ClearOrder()
        {
            while (OrderList != null && !OrderList.IsEmpty)
            {
                OrderList.TryDequeue(out _);
            }
        }
        private void ClearCommand()
        {
            while (CommandList != null && !CommandList.IsEmpty)
            {
                CommandList.TryDequeue(out _);
            }
        }

        /// <summary>
        /// 精确检查对应动画是否存在
        /// </summary>
        /// <param name="name">动画名</param>
        /// <param name="mode">模式</param>
        /// <returns></returns>
        public bool Exist(string name, string mode)
        {
            return animations.Where(x => x.name.ToLower().Contains(name) && x.name.ToLower().Contains(mode.ToLower())).Any();
        }

        /// <summary>
        /// 请求播放动画
        /// </summary>
        /// <param name="name">动画名</param>
        /// <param name="mode">模式</param>
        /// <param name="loopTimes">预约循环次数,最大支持1000</param>
        /// <param name="forceExit">强制打断上一个动画</param>
        /// <param name="callback">动画版播放完成时的回调</param>
        public void PlayAnimation(string graph, string name, string mode, uint loopTimes = 0, bool forceExit = true, Action callback = null)
        {
            loopTimes = Math.Min(loopTimes, 1000);
            if (forceExit)
            {
                repeatFlag = false;
                repeatTimes = 0;
                currentPlayingCommand = () => OrderAnimation(graph, name, mode, loopTimes, forceExit, callback);
                currentPlayingCommand.Invoke();
            }
            else
            {
                Action action = () => OrderAnimation(graph, name, mode, loopTimes, forceExit, callback);
                CommandList.Enqueue(action);
                if (ProcessOrderThread.ThreadState == ThreadState.WaitSleepJoin) ProcessOrderThread.Interrupt();
            }
        }

        /// <summary>
        /// 循环当前动画
        /// </summary>
        /// <param name="times">循环次数，-1为无限循环，直到有新动作打断</param>
        public void RepeatCurrentAnimation(int times = 1)
        {
            repeatTimes = times;
            repeatFlag = times != 0;
        }

        public void Dispose()
        {
            ProcessOrderThread?.Abort();
            currentPlayingCommand = null;

            ClearCommand();
            ClearOrder();

            if (graphs != null)
            {
                graphs.ToList().ForEach(x => x.Value.Clear());
                graphs.Clear();
                graphs = null;
            }

            animations?.ForEach(x => x.Dispose());
            animations?.Clear();
            animations = null;

            framesCache?.Clear();
            framesCache = null;
        }
    }
}
