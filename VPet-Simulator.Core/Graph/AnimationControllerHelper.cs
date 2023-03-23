using System.Text.RegularExpressions;
using System.Xml.Linq;
using static VPet_Simulator.Core.GraphCore;

namespace VPet_Simulator.Core
{
    public static class AnimationControllerHelper
    {
        public static string GetGrpahString(this GraphType graphType)
        {
            return graphType.ToString().Replace("_Start", "").Replace("_Loop", "").Replace("_End", "").ToLower();
        }

        static readonly string[] existSegment = { "_A(?=_|$)", "_B(?=_|$)", "_C(?=_|$)" };
        public static bool IsAnimationFamily(string a, string b)
        {
            foreach (var s in existSegment)
            {
                a = Regex.Replace(a, s, "", RegexOptions.IgnoreCase);
                b = Regex.Replace(b, s, "", RegexOptions.IgnoreCase);
            }
            return a == b;
        }
    }
}