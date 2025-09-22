using Emulator;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace SharpNES
{
    /// <summary>
    /// Interaction logic for SoundDebugger.xaml
    /// </summary>
    public partial class SoundDebugger : Window
    {
        private static SoundDebugger? _instance;

        public SoundDebugger(SharpNesEmu emulator)
        {
            InitializeComponent();
            _instance = this;

            emulator.CPU.APURegisterWrite += (s, e) =>
            {
                LogAPUWrite(e.Register, e.Value);
            };
            emulator.CPU.APURegisterRead += (s, e) =>
            {
                // Optionally log reads if needed
            };
        }

        /// <summary>
        /// Logs an APU register write to the debug window
        /// </summary>
        /// <param name="address">Memory address (e.g., 0x4000)</param>
        /// <param name="value">Value written to the register</param>
        public static void LogAPUWrite(uint address, int value)
        {
            if (_instance == null) return;

            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var registerName = GetRegisterName(address);
            var logEntry = $"[{timestamp}] 0x{address:X4} ({registerName}) <- 0x{value:X2} ({value:D3})\n";

            // Use Dispatcher to ensure UI updates happen on the UI thread
            _instance.Dispatcher.Invoke(() =>
            {
                _instance.APULogTextBox.AppendText(logEntry);
                _instance.APULogTextBox.ScrollToEnd();
            });
        }

        private static string GetRegisterName(uint address)
        {
            return address switch
            {
                0x00 => "PULSE1_CTRL",
                0x01 => "PULSE1_SWEEP",
                0x02 => "PULSE1_TIMER_LO",
                0x03 => "PULSE1_TIMER_HI",
                0x04 => "PULSE2_CTRL",
                0x05 => "PULSE2_SWEEP",
                0x06 => "PULSE2_TIMER_LO",
                0x07 => "PULSE2_TIMER_HI",
                0x08 => "TRIANGLE_CTRL",
                0x09 => "TRIANGLE_UNUSED",
                0x0A => "TRIANGLE_TIMER_LO",
                0x0B => "TRIANGLE_TIMER_HI",
                0x0C => "NOISE_CTRL",
                0x0D => "NOISE_UNUSED",
                0x0E => "NOISE_PERIOD",
                0x0F => "NOISE_LENGTH",
                0x10 => "DMC_CTRL",
                0x11 => "DMC_OUTPUT",
                0x12 => "DMC_ADDR",
                0x13 => "DMC_LENGTH",
                0x15 => "APU_STATUS",
                0x17 => "FRAME_COUNTER",
                _ => "UNKNOWN"
            };
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            APULogTextBox.Clear();
            APULogTextBox.AppendText("APU Register Write Log - Cleared\n");
        }

        private void SaveLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    DefaultExt = "txt",
                    FileName = $"APU_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveFileDialog.FileName, APULogTextBox.Text);
                    MessageBox.Show($"Log saved to: {saveFileDialog.FileName}", "Save Successful",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving log: {ex.Message}", "Save Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _instance = null;
            base.OnClosed(e);
        }
    }
}