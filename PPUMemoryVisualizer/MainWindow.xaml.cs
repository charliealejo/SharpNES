using Emulator;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PPUMemoryVisualizer
{
    public partial class MainWindow : Window
    {
        private readonly SharpNesEmu _emulator;

        public MainWindow()
        {
            InitializeComponent();
            _emulator = new SharpNesEmu("testroms/nestest.nes", true, 0xC000);
            RenderPatternTable();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RenderPatternTable();
        }

        private void RenderPatternTable()
        {
            TileCanvas.Children.Clear();
            
            int patternTableOffset = PatternTableComboBox.SelectedIndex * 0x1000;
            int paletteIndex = PaletteComboBox.SelectedIndex;
            
            // Each pattern table has 16x16 tiles (256 total)
            for (int tileY = 0; tileY < 16; tileY++)
            {
                for (int tileX = 0; tileX < 16; tileX++)
                {
                    int tileIndex = tileY * 16 + tileX;
                    var tileImage = RenderTile(patternTableOffset, tileIndex, paletteIndex);
                    
                    var image = new Image
                    {
                        Source = tileImage,
                        Width = 32, // Scale up 8x8 to 32x32 for visibility
                        Height = 32
                    };
                    
                    Canvas.SetLeft(image, tileX * 34); // 32 + 2 pixel spacing
                    Canvas.SetTop(image, tileY * 34);
                    
                    TileCanvas.Children.Add(image);
                }
            }
        }

        private WriteableBitmap RenderTile(int patternTableOffset, int tileIndex, int paletteIndex)
        {
            var bitmap = new WriteableBitmap(8, 8, 96, 96, PixelFormats.Bgra32, null);
            
            int tileOffset = patternTableOffset + (tileIndex * 16);
            
            bitmap.Lock();
            try
            {
                unsafe
                {
                    uint* pixels = (uint*)bitmap.BackBuffer;
                    
                    for (int y = 0; y < 8; y++)
                    {
                        byte lowByte = _emulator.PPU.Memory[tileOffset + y];
                        byte highByte = _emulator.PPU.Memory[tileOffset + y + 8];
                        
                        for (int x = 0; x < 8; x++)
                        {
                            int bit = 7 - x;
                            int colorIndex = ((highByte >> bit) & 1) << 1 | ((lowByte >> bit) & 1);
                            
                            // Use simple grayscale for now - you can enhance this with actual NES palettes
                            uint color = colorIndex switch
                            {
                                0 => 0xFF000000, // Black
                                1 => 0xFF555555, // Dark gray
                                2 => 0xFFAAAAAA, // Light gray
                                3 => 0xFFFFFFFF, // White
                                _ => 0xFF000000
                            };
                            
                            pixels[y * 8 + x] = color;
                        }
                    }
                }
                
                bitmap.AddDirtyRect(new Int32Rect(0, 0, 8, 8));
            }
            finally
            {
                bitmap.Unlock();
            }
            
            return bitmap;
        }
    }
}
