
namespace NESPPU
{
    public class Renderer
    {
        private const int Width = 256;
        private const int Height = 240;

        private PPU _ppu;

        public int[] FrameBuffer { get; private set; }

        public Renderer(PPU ppu)
        {
            _ppu = ppu;
            FrameBuffer = new int[Width * Height];
        }

        public void RenderPixel()
        {
            // Rendering logic for a single pixel
        }

        public void FetchBackgroundData()
        {
            // Fetch background data for the current scanline
        }
    }
}