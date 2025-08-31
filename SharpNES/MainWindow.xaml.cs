using Emulator;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SharpNES
{
    public partial class MainWindow : Window
    {
        private readonly SharpNesEmu _emulator;
        private readonly WriteableBitmap _bitmap;

        public MainWindow()
        {
            InitializeComponent();
            _emulator = new SharpNesEmu("testroms/nestest.nes");
            _emulator.PPU.FrameCompleted += OnFrameCompleted;

            // Initialize WriteableBitmap for NESViewer
            _bitmap = new WriteableBitmap(256, 240, 96, 96, PixelFormats.Bgr32, null);
            NESViewer.Source = _bitmap;

            Task.Run(() => _emulator.Run());
        }

        private void OnFrameCompleted(object? sender, int[] e)
        {
            // Update the WriteableBitmap with the new frame data
            Dispatcher.Invoke(() =>
            {
                _bitmap.WritePixels(new Int32Rect(0, 0, 256, 240), e, 256 * 4, 0);
            });
        }
    }
}
