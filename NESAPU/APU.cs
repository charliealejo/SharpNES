using NAudio.Wave;
using NESAPU.Channels;

namespace NESAPU
{
    public class APU : ISampleProvider
    {
        private readonly float _sampleRate;
        
        // APU Channels
        private readonly Pulse _pulse1;
        private readonly Pulse _pulse2;
        
        // Frame counter for timing
        private int _frameCounter;
        private int _frameSequence;
        private bool _interruptInhibit;
        private bool _sequencerMode; // false = 4-step, true = 5-step
        private bool _frameInterruptFlag;

        // Timing
        private int _frameCounterCycles;
        private const float FrameCounterRate = 240f; // 240Hz for envelope/triangle linear counter
        private const float LengthCounterRate = 120f; // 120Hz for length counter and sweep
        
        // Status register ($4015)
        private byte _status;
        
        // Event for frame interrupts
        public event EventHandler? FrameInterrupt;
        
        public WaveFormat WaveFormat { get; }

        public APU(float sampleRate = 44100f)
        {
            _sampleRate = sampleRate;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat((int)sampleRate, 1);
            
            _pulse1 = new Pulse(sampleRate);
            _pulse2 = new Pulse(sampleRate);
            
            Reset();
        }

        public void Reset()
        {
            _frameCounter = 0;
            _frameSequence = 0;
            _interruptInhibit = false;
            _sequencerMode = false;
            _frameCounterCycles = 0;
            _frameInterruptFlag = false;
            _status = 0;

            _pulse1.Reset();
            _pulse2.Reset();
        }

        public byte HandleRegisterRead(uint register)
        {
            if (register == 0x15)
            {
                return ReadStatus();
            }
            else if (register == 0x17)
            {
                return (byte)((_sequencerMode ? 0x80 : 0x00) | (_interruptInhibit ? 0x40 : 0x00));
            }
            else
            {
                // Other registers are write-only
                return 0;
            }
        }

        public void HandleRegisterWrite(uint register, byte value)
        {
            switch (register)
            {
                // Pulse 1 registers ($4000-$4003)
                case 0x00:
                case 0x01:
                case 0x02:
                case 0x03:
                    _pulse1.WriteRegister((int)(register & 3), value);
                    break;
                
                // Pulse 2 registers ($4004-$4007)
                case 0x04:
                case 0x05:
                case 0x06:
                case 0x07:
                    _pulse2.WriteRegister((int)(register & 3), value);
                    break;
                
                // Triangle ($4008-$400B) - Not implemented yet
                case 0x08:
                case 0x09:
                case 0x0A:
                case 0x0B:
                    // TODO: Implement triangle channel
                    break;
                
                // Noise ($400C-$400F) - Not implemented yet
                case 0x0C:
                case 0x0D:
                case 0x0E:
                case 0x0F:
                    // TODO: Implement noise channel
                    break;
                
                // DMC ($4010-$4013) - Not implemented yet
                case 0x10:
                case 0x11:
                case 0x12:
                case 0x13:
                    // TODO: Implement DMC channel
                    break;
                
                // Status register ($4015)
                case 0x15:
                    _status = value;
                    // Enable/disable channels based on status bits
                    if ((value & 0x01) == 0)
                    {
                        // Disable pulse 1 - set length counter to 0
                        _pulse1.SetLengthCounter(0); // Disable pulse 1
                    }
                    if ((value & 0x02) == 0)
                    {
                        // Disable pulse 2 - set length counter to 0
                        _pulse2.SetLengthCounter(0); // Disable pulse 1
                    }
                    break;
                
                // Frame counter ($4017)
                case 0x17:
                    _sequencerMode = (value & 0x80) != 0; // Bit 7: 0 = 4-step, 1 = 5-step
                    _interruptInhibit = (value & 0x40) != 0; // Bit 6: IRQ inhibit

                    _frameInterruptFlag = false; // Writing to $4017 clears frame interrupt flag

                    // Reset frame counter
                    _frameCounter = 0;
                    _frameSequence = 0;
                    
                    // If 5-step mode, immediately clock frame counter
                    if (_sequencerMode)
                    {
                        ClockFrameCounter();
                    }
                    break;
            }
        }

        // This should be called by the CPU at approximately 1.789773 MHz / 2 (APU cycles)
        public void Clock()
        {
            _frameCounterCycles++;

            // Frame counter runs at ~240Hz, which is every ~7457 APU cycles
            if (_frameCounterCycles >= 7457)
            {
                _frameCounterCycles = 0;
                ClockFrameCounter();
            }
        }

        private void ClockFrameCounter()
        {
            if (_sequencerMode) // 5-step mode
            {
                switch (_frameSequence)
                {
                    case 0:
                    case 2:
                        ClockEnvelopes();
                        break;
                    case 1:
                    case 3:
                        ClockEnvelopes();
                        ClockLengthCountersAndSweep();
                        break;
                    case 4:
                        // Do nothing
                        break;
                }
                
                _frameSequence = (_frameSequence + 1) % 5;
            }
            else // 4-step mode
            {
                switch (_frameSequence)
                {
                    case 0:
                    case 2:
                        ClockEnvelopes();
                        break;
                    case 1:
                        ClockEnvelopes();
                        ClockLengthCountersAndSweep();
                        break;
                    case 3:
                        ClockEnvelopes();
                        ClockLengthCountersAndSweep();
                        
                        // Generate frame interrupt if not inhibited
                        if (!_interruptInhibit)
                        {
                            _frameInterruptFlag = true;
                            FrameInterrupt?.Invoke(this, EventArgs.Empty);
                        }
                        break;
                }
                
                _frameSequence = (_frameSequence + 1) % 4;
            }
        }

        private void ClockEnvelopes()
        {
            _pulse1.ClockEnvelope();
            _pulse2.ClockEnvelope();
            // TODO: Clock triangle linear counter and noise envelope
        }

        private void ClockLengthCountersAndSweep()
        {
            _pulse1.ClockLength();
            _pulse1.ClockSweep();
            
            _pulse2.ClockLength();
            _pulse2.ClockSweep();
            
            // TODO: Clock triangle length counter and noise length counter
        }

        // ISampleProvider implementation for audio mixing
        public int Read(float[] buffer, int offset, int count)
        {
            // Create temporary buffers for each channel
            float[] pulse1Buffer = new float[count];
            float[] pulse2Buffer = new float[count];
            
            // Get samples from each channel
            _pulse1.Read(pulse1Buffer, 0, count);
            _pulse2.Read(pulse2Buffer, 0, count);
            
            // Mix the channels using NES APU mixing algorithm
            for (int i = 0; i < count; i++)
            {
                // NES APU uses a non-linear mixing algorithm
                float pulse1Sample = pulse1Buffer[i];
                float pulse2Sample = pulse2Buffer[i];
                
                // Mix pulse channels (simplified linear mixing for now)
                // The actual NES uses: pulse_out = 95.88 / (8128 / (pulse1 + pulse2) + 100)
                float pulseSum = pulse1Sample + pulse2Sample;
                float pulseOut = 0f;
                
                if (pulseSum > 0)
                {
                    // Simplified approximation of NES mixing
                    pulseOut = pulseSum * 0.5f; // Average and reduce volume
                }
                
                // TODO: Add triangle, noise, and DMC channels to the mix
                
                // Apply master volume and clamp
                buffer[offset + i] = Math.Clamp(pulseOut, -1.0f, 1.0f);
            }
            
            return count;
        }

        // Method to get current APU status (for $4015 reads)
        public byte ReadStatus()
        {
            byte status = 0;

            if (_pulse1.IsActive()) status |= 0x01; // Pulse 1 length counter > 0
            if (_pulse2.IsActive()) status |= 0x02; // Pulse 2 length counter > 0

            // Frame interrupt flag (bit 6)
            if (_frameInterruptFlag) status |= 0x40;

            // Clear frame interrupt flag when reading status
            _frameInterruptFlag = false;

            // TODO: Add triangle, noise, and DMC status bits
            // Bit 2: Triangle length counter > 0
            // Bit 3: Noise length counter > 0
            // Bit 4: DMC active
            // Bit 7: DMC interrupt

            return status;
        }
    }
}
