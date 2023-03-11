using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VPet_Simulator.Core.New;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// PNGAnimation.xaml 的交互逻辑
    /// </summary>
    public partial class PNGAnimation : IGraph
    {
        /// <summary>
        /// 所有动画帧
        /// </summary>
        public List<Animation> Animations;
        /// <summary>
        /// 当前动画播放状态
        /// </summary>
        public bool PlayState { get; set; } = false;
        /// <summary>
        /// 当前动画是否执行ENDACTION
        /// </summary>
        private bool DoEndAction = true;
        /// <summary>
        /// 是否循环播放
        /// </summary>
        public bool IsLoop { get; set; }
        /// <summary>
        /// 是否循环播放
        /// </summary>
        public bool IsContinue { get; set; } = false;
        ///// <summary>
        ///// 是否重置状态从0开始播放
        ///// </summary>
        //public bool IsResetPlay { get; set; } = false;
        public UIElement This => this;

        public Save.ModeType ModeType { get; private set; }

        public GraphCore.GraphType GraphType { get; private set; }
        /// <summary>
        /// 是否准备完成
        /// </summary>
        public bool IsReady { get; private set; } = false;

        public string name
        {
            get
            {
                return this.ModeType.ToString();
            }
        }

        /// <summary>
        /// 动画停止时运行的方法
        /// </summary>
        private Action StopAction;
        int nowid;
        /// <summary>
        /// 新建 PNG 动画
        /// </summary>
        /// <param name="path">文件夹位置</param>
        /// <param name="paths">文件内容列表</param>
        /// <param name="isLoop">是否循环</param>
        public PNGAnimation(string path, FileInfo[] paths, Save.ModeType modetype, GraphCore.GraphType graphtype, bool isLoop = false)
        {
            InitializeComponent();
            Animations = new List<Animation>();
            IsLoop = isLoop;
            GraphType = graphtype;
            ModeType = modetype;
            Task.Run(() => startup(path, paths));
        }
        private void startup(string path, FileInfo[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                FileInfo file = paths[i];
                int time = int.Parse(file.Name.Split('.').Reverse().ToArray()[1].Split('_').Last());
                Animations.Add(new Animation(this, time, () => { }, paths[i].FullName));
            }
            IsReady = true;
        }

        /// <summary>
        /// 单帧动画
        /// </summary>
        public class Animation
        {
            private PNGAnimation parent;
            /// <summary>
            /// 显示
            /// </summary>
            public Action Visible;
            ///// <summary>
            ///// 隐藏
            ///// </summary>
            //public Action Hidden;
            /// <summary>
            /// 帧时间
            /// </summary>
            public int Time;
            public string path;
            public Animation(PNGAnimation parent, int time, Action visible, string path)//, Action hidden)
            {
                this.path = path;
                this.parent = parent;
                Time = time;
                Visible = visible;
                //Hidden = hidden;
            }
            /// <summary>
            /// 运行该图层
            /// </summary>`
            public void Run(Action EndAction = null)
            {
                return;
                //先显示该图层
                parent.Dispatcher.Invoke(Visible);
                //AnimationController.Instance.PlayRawFrame(path);
                //然后等待帧时间毫秒
                Thread.Sleep(Time);
                //判断是否要下一步
                if (parent.PlayState)
                {
                    if (++parent.nowid >= parent.Animations.Count)
                        if (parent.IsLoop)
                            parent.nowid = 0;
                        else if (parent.IsContinue)
                        {
                            parent.IsContinue = false;
                            parent.nowid = 0;
                        }
                        else
                        {
                            parent.PlayState = false;
                            if (parent.DoEndAction)
                                EndAction?.Invoke();//运行结束动画时事件
                            parent.StopAction?.Invoke();
                            parent.StopAction = null;
                            return;
                        }
                    parent.Animations[parent.nowid].Run(EndAction);
                    return;
                }
                else
                {
                    parent.IsContinue = false;
                    if (parent.DoEndAction)
                        EndAction?.Invoke();//运行结束动画时事件
                    parent.StopAction?.Invoke();
                    parent.StopAction = null;

                }
            }
        }


        private static List<Action> taskQuene = new List<Action>();
        /// <summary>
        /// 从0开始运行该动画
        /// </summary>
        public void Run(Action EndAction = null)
        {
            //if(endwilldo != null && nowid != Animations.Count)
            //{
            //    endwilldo.Invoke();
            //    endwilldo = null;
            //}
            if (PlayState)
            {//如果当前正在运行,重置状态
                //IsResetPlay = true;
                Stop(true);
                StopAction = () => Run(EndAction);
                return;
            }
            nowid = 0;
            PlayState = true;
            DoEndAction = true;
            new Thread(() => Animations[0].Run(EndAction)).Start();
        }

        public void Stop(bool StopEndAction = false)
        {
            DoEndAction = !StopEndAction;
            PlayState = false;
            //IsResetPlay = false;
        }

        public void WaitForReadyRun(Action EndAction = null)
        {
            Task.Run(() =>
            {
                while (!IsReady)
                {
                    Thread.Sleep(100);
                }
                Run(EndAction);
            });
        }
    }
}
