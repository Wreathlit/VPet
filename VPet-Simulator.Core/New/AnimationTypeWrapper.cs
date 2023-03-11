using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VPet_Simulator.Core.GraphCore;

namespace VPet_Simulator.Core.New
{
    public static class AnimationTypeWrapper
    {
        public static string GetGrpahString(this GraphType graphType)
        {
            return graphType.ToString().Replace("_Start", "").Replace("_Loop", "").Replace("_End", "").ToLower();
        }
    }
}
