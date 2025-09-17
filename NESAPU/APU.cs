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
        private readonly Triangle _triangle;
        private readonly Noise _noise;
        private readonly DMC _dmc;
        
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
            _triangle = new Triangle(sampleRate);
            _noise = new Noise(sampleRate);
            _dmc = new DMC(sampleRate);

            Reset();
        }

        /// <summary>
        /// Sets the memory read delegate for the DMC channel.
        /// This should be called by the system to provide memory access.
        /// </summary>
        public void SetDmcMemoryReader(DMC.MemoryReadDelegate memoryReader)
        {
            _dmc.MemoryRead = memoryReader;
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
            _triangle.Reset();
            _noise.Reset();
            _dmc.Reset();
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
                
                // Triangle ($4008-$400B)
                case 0x08:
                case 0x09:
                case 0x0A:
                case 0x0B:
                    _triangle.WriteRegister((int)(register & 3), value);
                    break;
                
                // Noise ($400C-$400F)
                case 0x0C:
                case 0x0D:
                case 0x0E:
                case 0x0F:
                    _noise.WriteRegister((int)(register & 3), value);
                    break;
                
                // DMC ($4010-$4013)
                case 0x10:
                case 0x11:
                case 0x12:
                case 0x13:
                    _dmc.WriteRegister((int)(register & 3), value);
                    break;
                
                // Status register ($4015)
                case 0x15:
                    _status = value;
                    // Enable/disable channels based on status bits
                    if ((value & 0x01) == 0)
                    {
                        // Disable pulse 1 - set length counter to 0
                        _pulse1.SetLengthCounter(0);
                    }
                    if ((value & 0x02) == 0)
                    {
                        // Disable pulse 2 - set length counter to 0
                        _pulse2.SetLengthCounter(0);
                    }
                    if ((value & 0x04) == 0)
                    {
                        // Disable triangle - set length counter to 0
                        _triangle.SetLengthCounter(0);
                    }
                    if ((value & 0x08) == 0)
                    {
                        _noise.SetLengthCounter(0); // Disable noise
                    }
                    if ((value & 0x10) != 0)
                    {
                        // Enable DMC - start sample playback
                        _dmc.StartSample();
                    }
                    else
                    {
                        // Disable DMC - stop sample playback
                        _dmc.StopSample();
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

            // Frame counter runs at ~240Hz, which is every ~3728 APU cycles
            if (_frameCounterCycles >= 3728)
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
            _triangle.ClockLinearCounter();
            _noise.ClockEnvelope();
        }

        private void ClockLengthCountersAndSweep()
        {
            _pulse1.ClockLength();
            _pulse1.ClockSweep();
            
            _pulse2.ClockLength();
            _pulse2.ClockSweep();

            _triangle.ClockLength();

            _noise.ClockLength();
        }

        // ISampleProvider implementation for audio mixing
        public int Read(float[] buffer, int offset, int count)
        {
            // Create temporary buffers for each channel
            float[] pulse1Buffer = new float[count];
            float[] pulse2Buffer = new float[count];
            float[] triangleBuffer = new float[count];
            float[] noiseBuffer = new float[count];
            float[] dmcBuffer = new float[count];

            // Get samples from each channel
            _pulse1.Read(pulse1Buffer, 0, count);
            _pulse2.Read(pulse2Buffer, 0, count);
            _triangle.Read(triangleBuffer, 0, count);
            _noise.Read(noiseBuffer, 0, count);
            _dmc.Read(dmcBuffer, 0, count);

            // Mix the channels using NES APU mixing algorithm
            for (int i = 0; i < count; i++)
            {
                // NES APU uses a non-linear mixing algorithm
                float pulse1Sample = pulse1Buffer[i];
                float pulse2Sample = pulse2Buffer[i];
                float triangleSample = triangleBuffer[i];
                float noiseSample = noiseBuffer[i];
                float dmcSample = dmcBuffer[i];

                // Mix all channels (simplified linear mixing)
                float totalSum = pulse1Sample + pulse2Sample + triangleSample + noiseSample + dmcSample;
                float mixedOutput = totalSum * 0.2f; // Average and reduce volume
                
                // Apply master volume and clamp
                buffer[offset + i] = Math.Clamp(mixedOutput, -1.0f, 1.0f);
            }
            
            return count;
        }

        // Method to get current APU status (for $4015 reads)
        public byte ReadStatus()
        {
            byte status = 0;

            if (_pulse1.IsActive()) status |= 0x01; // Pulse 1 length counter > 0
            if (_pulse2.IsActive()) status |= 0x02; // Pulse 2 length counter > 0
            if (_triangle.IsActive()) status |= 0x04; // Triangle length counter > 0
            if (_noise.IsActive()) status |= 0x08; // Noise length counter > 0
            if (_dmc.IsActive()) status |= 0x10; // DMC active

            // Frame interrupt flag (bit 6)
            if (_frameInterruptFlag) status |= 0x40;

            // DMC interrupt flag (bit 7)
            if (_dmc.GetInterruptFlag()) status |= 0x80;

            // Clear frame interrupt flag when reading status
            _frameInterruptFlag = false;
            
            // Clear DMC interrupt flag when reading status
            _dmc.ClearInterruptFlag();

            return status;
        }
    }
}
