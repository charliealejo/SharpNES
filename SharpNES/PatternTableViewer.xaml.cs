using Emulator;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SharpNES
{
    /// <summary>
    /// Interaction logic for PatternTableViewer.xaml
    /// </summary>
    public partial class PatternTableViewer : Window
    {
        private readonly SharpNesEmu _emulator;

        public PatternTableViewer(SharpNesEmu emulator)
        {
            InitializeComponent();

            _emulator = emulator;

            GeneratePatternTableImage();


            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += GeneratePatternTableImage_Tick;
            timer.Start();
        }

        private void GeneratePatternTableImage_Tick(object? sender, EventArgs e)
        {
            if (IsVisible)
            {
                GeneratePatternTableImage();
            }
        }

        private void GeneratePatternTableImage()
        {
            // Generate an image from the pattern tables
            var image = new WriteableBitmap(256, 128, 96, 96, PixelFormats.Bgra32, null);
            var pixels = new byte[256 * 128 * 4];
            for (int patternTable = 0; patternTable < 2; patternTable++)
            {
                ushort baseAddress = (ushort)(patternTable * 0x1000); // 0x0000 for table 0, 0x1000 for table 1
                int xOffset = patternTable * 128; // Offset for the second table

                for (int tileY = 0; tileY < 16; tileY++)
                {
                    for (int tileX = 0; tileX < 16; tileX++)
                    {
                        int tileIndex = tileY * 16 + tileX;
                        for (int row = 0; row < 8; row++)
                        {
                            byte lowByte = _emulator.PPU.ReadMemory((ushort)(baseAddress + tileIndex * 16 + row));
                            byte highByte = _emulator.PPU.ReadMemory((ushort)(baseAddress + tileIndex * 16 + row + 8));
                            for (int col = 0; col < 8; col++)
                            {
                                int bit = 7 - col;
                                int colorIndex = ((highByte >> bit) & 1) << 1 | ((lowByte >> bit) & 1);
                                int pixelX = xOffset + tileX * 8 + col;
                                int pixelY = tileY * 8 + row;
                                int pixelIndex = (pixelY * 256 + pixelX) * 4;

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
            image.WritePixels(new Int32Rect(0, 0, 256, 128), pixels, 256 * 4, 0);
            PatternTableImage.Source = image;
        }
    }
}
