using Emulator;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SharpNES
{
    /// <summary>
    /// Interaction logic for NametablesViewer.xaml
    /// </summary>
    public partial class NametablesViewer : Window
    {
        private readonly SharpNesEmu _emulator;

        public NametablesViewer(SharpNesEmu emulator)
        {
            InitializeComponent();

            _emulator = emulator;

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += GenerateNametablesImage_Tick;
            timer.Start();
        }

        private void GenerateNametablesImage_Tick(object? sender, EventArgs e)
        {
            if (IsVisible)
            {
                GenerateNametablesImage();
            }
        }

        private void GenerateNametablesImage()
        {
            // Generate an image from the nametable and pattern tables
            var image = new WriteableBitmap(256 * 2, 240 * 2, 96, 96, PixelFormats.Bgra32, null);
            var pixels = new byte[256 * 2 * 240 * 2 * 4];

            for (int nametable = 0; nametable < 4; nametable++)
            {
                ushort baseAddress = (ushort)(0x2000 + nametable * 0x400);
                
                // Calculate offset for each nametable in the 2x2 grid
                int offsetX = (nametable % 2) * 256;
                int offsetY = (nametable / 2) * 240;

                ushort basePatternTableAddress = _emulator.PPU.Registers.F.BackgroundPatternTableAddress
                    ? (ushort)0x1000
                    : (ushort)0x0000;

                for (int posX = 0; posX < 32; posX++)
                {
                    for (int posY = 0; posY < 30; posY++)
                    {
                        // Read the tile index for this position
                        int tileIndex = _emulator.PPU.ReadMemory((ushort)(baseAddress + posY * 32 + posX));

                        // Render tile for position
                        for (int row = 0; row < 8; row++)
                        {
                            byte lowByte = _emulator.PPU.ReadMemory((ushort)(basePatternTableAddress + tileIndex * 16 + row));
                            byte highByte = _emulator.PPU.ReadMemory((ushort)(basePatternTableAddress + tileIndex * 16 + row + 8));
                            for (int col = 0; col < 8; col++)
                            {
                                int bit = 7 - col;
                                int colorIndex = ((highByte >> bit) & 1) << 1 | ((lowByte >> bit) & 1);
                                
                                // Calculate pixel position in the full 512x480 image
                                int pixelX = offsetX + posX * 8 + col;
                                int pixelY = offsetY + posY * 8 + row;
                                int pixelIndex = (pixelY * (256 * 2) + pixelX) * 4;

                                // Simple grayscale palette
                                byte colorValue = (byte)(colorIndex * 85); // 0, 85, 170, 255
                                pixels[pixelIndex + 0] = colorValue; // Blue
                                pixels[pixelIndex + 1] = colorValue; // Green
                                pixels[pixelIndex + 2] = colorValue; // Red
                                pixels[pixelIndex + 3] = 255;        // Alpha
                            }
                        }
                    }
                }
            }

            image.WritePixels(new Int32Rect(0, 0, 256 * 2, 240 * 2), pixels, (256 * 2) * 4, 0);
            NametablesImage.Source = image;
        }
    }
}
