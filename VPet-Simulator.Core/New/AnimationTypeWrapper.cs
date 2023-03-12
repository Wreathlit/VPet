using static VPet_Simulator.Core.GraphCore;

namespace VPet_Simulator.Core
{
    public static class AnimationTypeWrapper
    {
        public static string GetGrpahString(this GraphType graphType)
        {
            return graphType.ToString().Replace("_Start", "").Replace("_Loop", "").Replace("_End", "").ToLower();
        }
    }
}
