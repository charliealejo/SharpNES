using Emulator;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SharpNES
{
    public partial class MainWindow : Window
    {
        private readonly SharpNesEmu _emulator;
        private readonly WriteableBitmap _bitmap;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
            _emulator = new SharpNesEmu("testroms/nestest.nes");
            _emulator.PPU.FrameCompleted += OnFrameCompleted;
            _cancellationTokenSource = new CancellationTokenSource();

            // Initialize WriteableBitmap for NESViewer
            _bitmap = new WriteableBitmap(256, 240, 96, 96, PixelFormats.Bgr32, null);
            NESViewer.Source = _bitmap;

            // Handle window closing
            Closing += MainWindow_Closing;

            Task.Run(() => _emulator.Run());
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            // Unsubscribe from the event to prevent further calls
            _emulator.PPU.FrameCompleted -= OnFrameCompleted;

            // Cancel the emulator task
            _cancellationTokenSource.Cancel();
        }

        private void OnFrameCompleted(object? sender, int[] e)
        {
            // Check if cancellation was requested
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                return;

            // Update the WriteableBitmap with the new frame data
            try
            {
                Dispatcher.Invoke(() =>
                {
                    _bitmap.WritePixels(new Int32Rect(0, 0, 256, 240), e, 256 * 4, 0);
                });
            }
            catch (TaskCanceledException)
            {
                // Expected when the application is shutting down
            }
            catch (InvalidOperationException)
            {
                // Expected when the dispatcher is shutting down
            }
        }
    }
}
