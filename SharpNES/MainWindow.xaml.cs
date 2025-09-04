using Emulator;
using InputDevices;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
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
        private DispatcherTimer _debugUpdateTimer;

        private SharpNesEmu _emulator;
        private Task? _emulatorTask;

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

            // Initialize debug update timer
            _debugUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            _debugUpdateTimer.Tick += UpdateDebugInfo;
            _debugUpdateTimer.Start();

            // Handle window closing
            Closing += MainWindow_Closing;

            // Set focus to receive keyboard events
            Loaded += (s, e) => Focus();

            _emulatorTask = Task.Run(() => _emulator.Start());
        }

        private void UpdateDebugInfo(object? sender, EventArgs e)
        {
            if (_emulator?.PPU == null) return;

            var ppu = _emulator.PPU;
            var registers = ppu.Registers;
            var flags = registers.F;

            // Basic scroll values
            XScroll.Text = flags.HorizontalScroll.ToString();
            YScroll.Text = flags.VerticalScroll.ToString();
            PPUAddrText.Text = $"${flags.PPUAddress:X4}";

            // Status Flags
            VBlankText.Text = flags.VBlank.ToString();
            Sprite0HitText.Text = flags.Sprite0Hit.ToString();
            SpriteOverflowText.Text = flags.SpriteOverflow.ToString();
            NMIEnabledText.Text = flags.NMIEnabled.ToString();

            // Rendering Flags
            ShowBackgroundText.Text = flags.ShowBackground.ToString();
            ShowSpritesText.Text = flags.ShowSprites.ToString();
            BGPatternText.Text = flags.BackgroundPatternTableAddress ? "$1000" : "$0000";
            SprPatternText.Text = flags.SpritePatternTableAddress ? "$1000" : "$0000";

            // Update additional debug information if TextBlocks exist
            UpdateAdditionalDebugInfo(ppu, registers, flags);
        }

        private void UpdateAdditionalDebugInfo(NESPPU.PPU ppu, NESPPU.Registers registers, NESPPU.Registers.Flags flags)
        {
            // Try to update additional fields if they exist in XAML
            try
            {
                // Timing Information
                if (FindName("ScanLineText") is TextBlock scanLineText)
                    scanLineText.Text = ppu.ScanLine.ToString();
                
                if (FindName("DotText") is TextBlock dotText)
                    dotText.Text = ppu.Dot.ToString();

                // Register Values
                if (FindName("PPUStatusText") is TextBlock ppuStatusText)
                    ppuStatusText.Text = $"${registers.PPUSTATUS:X2}";
                
                if (FindName("OAMAddrText") is TextBlock oamAddrText)
                    oamAddrText.Text = $"${registers.OAMADDR:X2}";
                
                if (FindName("BaseNametableText") is TextBlock baseNametableText)
                    baseNametableText.Text = $"${flags.BaseNametableAddress + 0x2000:X4}";

                // Current Tile Information (only during visible scanlines)
                if (ppu.ScanLine < 240 && ppu.Dot > 0 && ppu.Dot <= 256)
                {
                    var effectiveX = ppu.Dot - 1 + flags.HorizontalScroll;
                    var effectiveY = ppu.ScanLine + flags.VerticalScroll;
                    
                    var tileX = effectiveX / 8;
                    var tileY = effectiveY / 8;
                    
                    // Handle nametable wrapping
                    var nametableX = (tileX / 32) % 2;
                    var nametableY = (tileY / 30) % 2;
                    var currentNametable = (ushort)(0x2000 + (nametableY * 0x800) + (nametableX * 0x400));
                    
                    var localTileX = tileX % 32;
                    var localTileY = tileY % 30;
                    
                    if (FindName("CurrentTileXText") is TextBlock currentTileXText)
                        currentTileXText.Text = localTileX.ToString();
                    
                    if (FindName("CurrentTileYText") is TextBlock currentTileYText)
                        currentTileYText.Text = localTileY.ToString();

                    // Tile index and address
                    var tileAddr = (ushort)(currentNametable + (localTileY * 32) + localTileX);
                    var tileIndex = ppu.ReadMemory(tileAddr);
                    
                    if (FindName("TileIndexText") is TextBlock tileIndexText)
                        tileIndexText.Text = $"${tileIndex:X2}";
                    
                    if (FindName("TileAddressText") is TextBlock tileAddressText)
                        tileAddressText.Text = $"${tileAddr:X4}";

                    // Attribute table information
                    var attrX = localTileX / 4;
                    var attrY = localTileY / 4;
                    var attrAddr = (ushort)(currentNametable + 0x3C0 + (attrY * 8) + attrX);
                    var attrData = ppu.ReadMemory(attrAddr);
                    
                    if (FindName("AttributeDataText") is TextBlock attributeDataText)
                        attributeDataText.Text = $"${attrData:X2}";
                    
                    if (FindName("AttributeAddrText") is TextBlock attributeAddrText)
                        attributeAddrText.Text = $"${attrAddr:X4}";

                    // Palette calculation
                    var quadrantX = (localTileX % 4) / 2;
                    var quadrantY = (localTileY % 4) / 2;
                    var quadrant = quadrantY * 2 + quadrantX;
                    var paletteIndex = (attrData >> (quadrant * 2)) & 0x03;
                    var paletteAddr = (ushort)(0x3F01 + (paletteIndex * 4));
                    
                    if (FindName("PaletteAddrText") is TextBlock paletteAddrText)
                        paletteAddrText.Text = $"${paletteAddr:X4}";

                    // Current nametable
                    if (FindName("CurrentNametableText") is TextBlock currentNametableText)
                        currentNametableText.Text = $"${currentNametable:X4}";
                }
                else
                {
                    // Clear tile info when not in visible area
                    if (FindName("CurrentTileXText") is TextBlock currentTileXText)
                        currentTileXText.Text = "-";
                    if (FindName("CurrentTileYText") is TextBlock currentTileYText)
                        currentTileYText.Text = "-";
                    if (FindName("TileIndexText") is TextBlock tileIndexText)
                        tileIndexText.Text = "$--";
                    if (FindName("TileAddressText") is TextBlock tileAddressText)
                        tileAddressText.Text = "$----";
                    if (FindName("AttributeDataText") is TextBlock attributeDataText)
                        attributeDataText.Text = "$--";
                    if (FindName("AttributeAddrText") is TextBlock attributeAddrText)
                        attributeAddrText.Text = "$----";
                    if (FindName("PaletteAddrText") is TextBlock paletteAddrText)
                        paletteAddrText.Text = "$----";
                    if (FindName("CurrentNametableText") is TextBlock currentNametableText)
                        currentNametableText.Text = "$----";
                }

                // Additional control flags
                if (FindName("IncrementModeText") is TextBlock incrementModeText)
                    incrementModeText.Text = flags.IncrementBy32 ? "32" : "1";
                
                if (FindName("SpriteSizeText") is TextBlock spriteSizeText)
                    spriteSizeText.Text = flags.SpriteSize ? "8x16" : "8x8";
                
                if (FindName("GrayscaleText") is TextBlock grayscaleText)
                    grayscaleText.Text = flags.Grayscale.ToString();

                // Emphasis flags
                if (FindName("EmphasizeRedText") is TextBlock emphasizeRedText)
                    emphasizeRedText.Text = flags.EmphasizeRed.ToString();
                if (FindName("EmphasizeGreenText") is TextBlock emphasizeGreenText)
                    emphasizeGreenText.Text = flags.EmphasizeGreen.ToString();
                if (FindName("EmphasizeBlueText") is TextBlock emphasizeBlueText)
                    emphasizeBlueText.Text = flags.EmphasizeBlue.ToString();

                // Left edge clipping
                if (FindName("ShowBGLeftText") is TextBlock showBGLeftText)
                    showBGLeftText.Text = flags.ShowBackgroundLeft.ToString();
                if (FindName("ShowSprLeftText") is TextBlock showSprLeftText)
                    showSprLeftText.Text = flags.ShowSpritesLeft.ToString();

                // Mirroring type
                if (FindName("MirroringText") is TextBlock mirroringText)
                    mirroringText.Text = ppu.Mirroring.ToString();
            }
            catch
            {
                // Ignore errors if TextBlocks don't exist
            }
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
                _emulatorTask = Task.Run(() => _emulator.Start());
            }
        }

        private async Task StopCurrentEmulator()
        {
            // Unsubscribe from the event to prevent further calls
            _emulator.PPU.FrameCompleted -= OnFrameCompleted;

            // Stop the emulator
            _emulator.Stop();

            // Cancel the token to signal frame completion to stop processing
            _cancellationTokenSource.Cancel();

            // Wait for the emulator task to complete with a timeout
            if (_emulatorTask != null)
            {
                try
                {
                    await _emulatorTask.WaitAsync(TimeSpan.FromSeconds(5));
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
            _cancellationTokenSource.Dispose();
        }

        // Debug control button event handlers
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _emulator.Pause();
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            _emulator.Resume();
        }

        private void StepButton_Click(object sender, RoutedEventArgs e)
        {
            _emulator.StepToNextFrame();
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

        private async void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            // Stop the debug timer
            _debugUpdateTimer?.Stop();
            
            // Stop the emulator properly when closing
            await StopCurrentEmulator();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
