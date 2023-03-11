using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using VPet_Simulator.Core.New;
using static VPet_Simulator.Core.GraphCore;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// 宠物加载器
    /// </summary>
    public class PetLoader
    {
        /// <summary>
        /// 宠物图像
        /// </summary>
        public GraphCore Graph()
        {
            var g = new GraphCore();
            foreach (var p in path)
                LoadGraph(g, new DirectoryInfo(p), "");
            g.GraphConfig = Config;
            AnimationController.Instance.ArrangeAnimation();
            return g;
        }
        /// <summary>
        /// 图像位置
        /// </summary>
        public List<string> path = new List<string>();
        /// <summary>
        /// 宠物名字
        /// </summary>
        public string Name;
        /// <summary>
        /// 宠物介绍
        /// </summary>
        public string Intor;
        public GraphCore.Config Config;
        public PetLoader(LpsDocument lps, DirectoryInfo directory)
        {
            Name = lps.First().Info;
            Intor = lps.First()["intor"].Info;
            path.Add(directory.FullName + "\\" + lps.First()["path"].Info);
            Config = new Config(lps);
        }

        public static void LoadGraph(GraphCore graph, DirectoryInfo di, string path_name)
        {
            var list = di.EnumerateDirectories();
            if (list.Count() == 0)
            {

            }
            else if (File.Exists(di.FullName + @"\info.lps"))
            {//如果自带描述信息,则手动加载
             //TODO:
            }
            else
                foreach (var p in list)
                {
                    LoadGraph(graph, p, path_name + "_" + p.Name);
                    AnimationController.Instance.AddAnimation(p, path_name + "_" + p.Name);
                }
        }
    }
}
