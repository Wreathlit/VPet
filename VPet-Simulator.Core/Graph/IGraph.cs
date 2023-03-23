using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace VPet_Simulator.Core
{
    public interface IGraph
    {
        void Order(Stream stream);
        GraphicsDevice GetGraphicsDevice();
        void OrderTexture(Texture2D texture, bool disposePrevious = false);
        void Clear();
    }
}
