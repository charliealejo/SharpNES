using Emulator;
using InputDevices;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SharpNES
{
    public partial class MainWindow : Window
    {
        private readonly WriteableBitmap _bitmap;
        private readonly Dictionary<Key, NesControllerButtons> _keyMap;
        private CancellationTokenSource _cancellationTokenSource;

        private SharpNesEmu _emulator;
        private Task? _emulatorTask;
        private bool _isPaused = false;

        private PatternTableViewer _patternTableViewer;
        private NametablesViewer _nametablesViewer;
        private SoundDebugger _soundDebugger;

        public MainWindow()
        {
            InitializeComponent();
            _emulator = new SharpNesEmu("testroms/mario.nes");
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
                // Arrow keys and X,Z as alternative
                { Key.X, NesControllerButtons.A },
                { Key.Z, NesControllerButtons.B },
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

            _emulatorTask = Task.Run(() => _emulator.Start(_cancellationTokenSource.Token));

            // Initialize debug windows
            _patternTableViewer = new PatternTableViewer(_emulator);
            _nametablesViewer = new NametablesViewer(_emulator);
            _soundDebugger = new SoundDebugger(_emulator);

            // Initialize menu state
            UpdateMenuState();
        }

        private void UpdateMenuState()
        {
            // Update menu items based on pause state
            PauseMenuItem.IsEnabled = !_isPaused;
            ResumeMenuItem.IsEnabled = _isPaused;
        }

        private void OnFrameCompleted(object? sender, int[] e)
        {
            // Check if cancellation was requested
            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                return;
            }

            // Update the WriteableBitmap with the new frame data
            try
            {
                Dispatcher.BeginInvoke(() =>
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

        private async void OpenROM_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "NES ROM files (*.nes)|*.nes|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Stop the current emulator instance properly
                await StopCurrentEmulator();

                // Create a new emulator instance with the selected ROM
                _emulator = new SharpNesEmu(openFileDialog.FileName);
                _emulator.PPU.FrameCompleted += OnFrameCompleted;

                // Create new cancellation token source for the new emulator
                _cancellationTokenSource = new CancellationTokenSource();

                // Start the new emulator task
                _emulatorTask = Task.Run(() => _emulator.Start(_cancellationTokenSource.Token));

                // Recreate debug windows with the new emulator instance
                _patternTableViewer = new PatternTableViewer(_emulator);
                _nametablesViewer = new NametablesViewer(_emulator);
                _soundDebugger = new SoundDebugger(_emulator);

                // Reset pause state
                _isPaused = false;
            }
        }

        private async Task StopCurrentEmulator()
        {
            // Unsubscribe from the event to prevent further calls
            if (_emulator?.PPU != null)
            {
                _emulator.PPU.FrameCompleted -= OnFrameCompleted;
            }

            // Stop the emulator
            _emulator?.Stop();

            // Cancel the token to signal frame completion to stop processing
            _cancellationTokenSource?.Cancel();

            // Wait for the emulator task to complete with a timeout
            if (_emulatorTask != null)
            {
                try
                {
                    await _emulatorTask.WaitAsync(TimeSpan.FromSeconds(2));
                }
                catch (TimeoutException)
                {
                    // Log or handle timeout if needed
                    System.Diagnostics.Debug.WriteLine("Emulator task did not complete within timeout period");
                }
                catch (TaskCanceledException)
                {
                    // Expected when task is cancelled
                }
            }

            // Dispose the old cancellation token source
            _cancellationTokenSource?.Dispose();

            // Close debug windows if they exist
            try
            {
                _patternTableViewer?.Close();
            }
            catch (InvalidOperationException)
            {
                // Window might already be closed
            }

            try
            {
                _nametablesViewer?.Close();
            }
            catch (InvalidOperationException)
            {
                // Window might already be closed
            }
        }

        // Debug control button event handlers
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _emulator.Pause();
            _isPaused = true;
            UpdateMenuState();
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            _emulator.Resume();
            _isPaused = false;
            UpdateMenuState();
        }

        private void StepButton_Click(object sender, RoutedEventArgs e)
        {
            _emulator.StepToNextFrame();
        }

        private void PatternTableViewerButton_Click(object sender, RoutedEventArgs e)
        {
            _patternTableViewer?.Show();
        }

        private void NametablesViewerButton_Click(object sender, RoutedEventArgs e)
        {
            _nametablesViewer?.Show();
        }

        private void SoundDebuggerButton_Click(object sender, RoutedEventArgs e)
        {
            _soundDebugger?.Show();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Handle function keys for debug controls
            switch (e.Key)
            {
                case Key.F5:
                    // F5 toggles pause/resume
                    if (_isPaused)
                    {
                        _emulator.Resume();
                        _isPaused = false;
                    }
                    else
                    {
                        _emulator.Pause();
                        _isPaused = true;
                    }
                    UpdateMenuState();
                    e.Handled = true;
                    return;

                case Key.F10:
                    // F10 steps to next frame
                    _emulator.StepToNextFrame();
                    e.Handled = true;
                    return;
            }

            // Handle NES controller inputs
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

        private async void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            // Stop the emulator properly when closing
            await StopCurrentEmulator();

            // Close any remaining debug windows safely
            try
            {
                _patternTableViewer?.Close();
            }
            catch (InvalidOperationException)
            {
                // Window might already be closed
            }

            try
            {
                _nametablesViewer?.Close();
            }
            catch (InvalidOperationException)
            {
                // Window might already be closed
            }

            try
            {
                _soundDebugger?.Close();
            }
            catch (InvalidOperationException)
            {
                // Window might already be closed
            }

            // Ensure all child windows are closed
            try
            {
                foreach (Window window in Application.Current.Windows.OfType<Window>().ToList())
                {
                    if (window != this && window.IsLoaded)
                    {
                        window.Close();
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Collection might be modified during enumeration
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
