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

        // Timing - these should be in CPU cycles, not APU cycles
        private int _frameCounterCycles;
        
        // Status register ($4015)
        private byte _status;
        
        // Event for frame interrupts
        public event EventHandler? FrameInterrupt;
        
        public WaveFormat WaveFormat { get; }

        public APU(float sampleRate = 44100f)
        {
            _sampleRate = sampleRate;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat((int)sampleRate, 1);
            
            _pulse1 = new Pulse(sampleRate, true);
            _pulse2 = new Pulse(sampleRate, false);
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

        // This should be called by the CPU at CPU frequency (every CPU cycle)
        public void Clock()
        {
            // Frame counter runs at ~240Hz from CPU clock
            // 4-step: cycles 7457, 14913, 22371, 29829 (and 29830 for IRQ)
            // 5-step: cycles 7457, 14913, 22371, 29829, 37281
            
            bool shouldClockQuarter = false;
            bool shouldClockHalf = false;
            
            if (_sequencerMode) // 5-step mode
            {
                switch (_frameCounter)
                {
                    case 7457:
                        shouldClockQuarter = true;
                        break;
                    case 14913:
                        shouldClockQuarter = true;
                        shouldClockHalf = true;
                        break;
                    case 22371:
                        shouldClockQuarter = true;
                        break;
                    case 29829:
                        break; // Do nothing
                    case 37281:
                        shouldClockQuarter = true;
                        shouldClockHalf = true;
                        _frameCounter = 0; // Reset counter
                        return;
                }
            }
            else // 4-step mode
            {
                switch (_frameCounter)
                {
                    case 7457:
                        shouldClockQuarter = true;
                        break;
                    case 14913:
                        shouldClockQuarter = true;
                        shouldClockHalf = true;
                        break;
                    case 22371:
                        shouldClockQuarter = true;
                        break;
                    case 29829:
                        shouldClockQuarter = true;
                        shouldClockHalf = true;
                        break;
                    case 29830:
                        // Generate frame interrupt if not inhibited
                        if (!_interruptInhibit)
                        {
                            _frameInterruptFlag = true;
                            FrameInterrupt?.Invoke(this, EventArgs.Empty);
                        }
                        _frameCounter = 0; // Reset counter
                        return;
                }
            }
            
            if (shouldClockQuarter)
            {
                ClockEnvelopes();
            }
            
            if (shouldClockHalf)
            {
                ClockLengthCountersAndSweep();
            }
            
            _frameCounter++;
        }

        private void ClockFrameCounter()
        {
            // This is called when writing to $4017 in 5-step mode
            ClockEnvelopes();
            ClockLengthCountersAndSweep();
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

            // Mix the channels using proper NES APU mixing algorithm
            for (int i = 0; i < count; i++)
            {
                // Convert to amplitude levels (0-15 for pulse/noise, 0-15 for triangle, 0-127 for DMC)
                float pulse1Level = Math.Abs(pulse1Buffer[i]) * 15f;
                float pulse2Level = Math.Abs(pulse2Buffer[i]) * 15f;
                float triangleLevel = Math.Abs(triangleBuffer[i]) * 15f;
                float noiseLevel = Math.Abs(noiseBuffer[i]) * 15f;
                float dmcLevel = Math.Abs(dmcBuffer[i]) * 127f;

                // NES APU mixing formula
                float pulseOut = 0f;
                if (pulse1Level + pulse2Level > 0)
                {
                    pulseOut = 95.88f / ((8128f / (pulse1Level + pulse2Level)) + 100f);
                }

                float tndOut = 0f;
                float tndSum = (triangleLevel / 8227f) + (noiseLevel / 12241f) + (dmcLevel / 22638f);
                if (tndSum > 0)
                {
                    tndOut = 159.79f / ((1f / tndSum) + 100f);
                }

                // Final output
                float finalOutput = pulseOut + tndOut;
                
                // Apply master volume and clamp
                buffer[offset + i] = Math.Clamp(finalOutput * 0.5f, -1.0f, 1.0f);
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
