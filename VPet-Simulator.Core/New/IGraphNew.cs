using System.IO;

namespace VPet_Simulator.Core
{
    public interface IGraphNew
    {
        void Order(Stream stream);
        void Clear();
    }
}
