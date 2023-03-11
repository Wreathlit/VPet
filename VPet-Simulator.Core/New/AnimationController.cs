using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace VPet_Simulator.Core.New
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
        //动画控制器，接收逻辑状态，输出动画状态
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
        private Thread ProcessOrderThread;
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
                            //Console.WriteLine("OrderFrame: " + a.ToString());
                            //Console.WriteLine(DateTime.Now.Second.ToString("00") + DateTime.Now.Millisecond.ToString("000") + "\tRender Request ============================>");
                            a.Invoke();
                        }
                        catch (ThreadInterruptedException e)
                        {
                            //Console.WriteLine("Interrupt ForceExit");
                        }
                        catch (ThreadAbortException e)
                        {
                            return;
                        }
                        finally
                        {

                        }
                    }
                }
                try
                {
                    Thread.Sleep(1000);
                }
                catch (ThreadInterruptedException e)
                {
                    //Console.WriteLine("Interrupt Awake");
                }
                catch (ThreadAbortException e)
                {
                    return;
                }
                finally
                {

                }
            }
        }

        private void OrderAnimation(string name, string mode, int loopTimes, bool forceExit, Action additionalAction)
        {
            AnimationInfo result;
            var accurate = rawAnimations.Where(x => x.name.ToLower().Contains(name) && x.name.ToLower().Contains(mode.ToLower())).ToList();
            if (accurate.Count > 0)
            {
                result = accurate.ElementAt(random.Next(accurate.Count));
                while (!OrderList.IsEmpty && forceExit)
                {
                    OrderList.TryDequeue(out _);
                }
                GenerateTasks(result).ForEach(x => OrderList.Enqueue(x));
                if (additionalAction != null)
                    OrderList.Enqueue(additionalAction);
                if (forceExit || ProcessOrderThread.ThreadState == ThreadState.WaitSleepJoin)
                    ProcessOrderThread.Interrupt();
                return;
            }
            var alter = rawAnimations.Where(x => x.name.ToLower().Contains(name) && x.name.ToLower().Contains("nomal")).ToList();

            if (alter.Count > 0)
            {
                result = alter.ElementAt(random.Next(alter.Count));
                while (!OrderList.IsEmpty && forceExit)
                {
                    OrderList.TryDequeue(out _);
                }
                GenerateTasks(result).ForEach(x => OrderList.Enqueue(x));
                if (additionalAction != null)
                    OrderList.Enqueue(additionalAction);
                if (forceExit || ProcessOrderThread.ThreadState == ThreadState.WaitSleepJoin)
                    ProcessOrderThread.Interrupt();
                return;
            }

            var protect = rawAnimations.Where(x => x.name.ToLower().Contains(name)).ToList();
            var last = rawAnimations.Where(x => x.name.ToLower().Contains("default")).ToList();
        }

        private List<Action> GenerateTasks(AnimationInfo animation)
        {
            List<Action> tasks = new List<Action>();

            for (int i = 0; i < animation.framesLoop.Count; i++)
            {
                FrameInfo f = animation.framesLoop[i];
                tasks.Add(() =>
                {
                    Task.Run(() => graph.Order(f.stream));
                    Thread.Sleep(f.frameTime);
                });
            }
            return tasks;
        }

        public void PlayAnimation(string name, string mode, int loopTimes = 0, bool forceExit = false, Action additionalAction = null)
        {


            OrderAnimation(name, mode, loopTimes, forceExit, additionalAction);
        }

        public void PlayRawFrame(string frameName)
        {
            FrameInfo f;
            if (framesCache.TryGetValue(frameName, out f))
            {
                //Console.WriteLine("Hit Cache");
                //graph.Order(f.stream);
            }
        }

        public void Dispose()
        {
            graph?.Clear();
            while (OrderList != null && !OrderList.IsEmpty)
            {
                OrderList.TryDequeue(out _);
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

        public void DrawSampleFrame()
        {
            if (graph == null)
            {
                throw new ArgumentNullException("需要先使用RegistryGraph注册画布");
            }

            var ts = new ThreadStart(testDrawThread);
            var t = new Thread(testDrawThread);
            t.Start();
        }

        public void testDrawThread()
        {
            int i = 0;
            Random rnd = new Random();
            while (true)
            {
                Thread.Sleep(100);
                var a = rawAnimations[rnd.Next(rawAnimations.Count - 1)];
                var f = a.framesLoop[i++ % a.framesLoop.Count];
                //Console.WriteLine("DrawNext: " + f.stream.Length);
                graph.Order(f.stream);
            }
        }
    }
}
