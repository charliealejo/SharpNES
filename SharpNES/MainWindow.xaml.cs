using Emulator;
using InputDevices;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SharpNES
{
    public partial class MainWindow : Window
    {
        private readonly SharpNesEmu _emulator;
        private readonly WriteableBitmap _bitmap;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Dictionary<Key, NesControllerButtons> _keyMap;

        public MainWindow()
        {
            InitializeComponent();
            _emulator = new SharpNesEmu("testroms/nestest.nes");
            _emulator.PPU.FrameCompleted += OnFrameCompleted;
            _cancellationTokenSource = new CancellationTokenSource();

            // Initialize key mapping
            _keyMap = new Dictionary<Key, NesControllerButtons>
            {
                { Key.M, NesControllerButtons.A },
                { Key.N, NesControllerButtons.B },
                { Key.Enter, NesControllerButtons.Start },
                { Key.Space, NesControllerButtons.Select },
                { Key.W, NesControllerButtons.Up },
                { Key.S, NesControllerButtons.Down },
                { Key.A, NesControllerButtons.Left },
                { Key.D, NesControllerButtons.Right },
                // Arrow keys as alternative
                { Key.Up, NesControllerButtons.Up },
                { Key.Down, NesControllerButtons.Down },
                { Key.Left, NesControllerButtons.Left },
                { Key.Right, NesControllerButtons.Right }
            };

            // Initialize WriteableBitmap for NESViewer
            _bitmap = new WriteableBitmap(256, 240, 96, 96, PixelFormats.Bgr32, null);
            NESViewer.Source = _bitmap;

            // Handle window closing
            Closing += MainWindow_Closing;

            // Set focus to receive keyboard events
            Loaded += (s, e) => Focus();

            Task.Run(() => _emulator.Run());
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (_keyMap.TryGetValue(e.Key, out var button))
            {
                _emulator.NesController.SetButtonState(button, true);
                e.Handled = true;
            }
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (_keyMap.TryGetValue(e.Key, out var button))
            {
                _emulator.NesController.SetButtonState(button, false);
                e.Handled = true;
            }
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
