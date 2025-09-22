using NAudio.Wave;

namespace NESAPU.Channels
{
    public class DMC : Channel
    {
        // DMC channel registers
        private byte _control;          // $4010 - IL-- RRRR (IRQ enable, Loop, Rate)
        private byte _directLoad;       // $4011 - -DDD DDDD (Direct load)
        private byte _sampleAddress;    // $4012 - AAAA AAAA (Sample address)
        private byte _sampleLength;     // $4013 - LLLL LLLL (Sample length)

        // Internal state
        private byte _outputLevel;      // 7-bit output level (0-127)
        private bool _silenceFlag;
        private int _shifterRegister;   // 8-bit shift register
        private int _bitsRemaining;     // Bits remaining in current byte
        private int _bytesRemaining;    // Bytes remaining in sample
        private ushort _currentAddress; // Current memory address being read
        private byte _sampleBuffer;     // Sample buffer
        private bool _sampleBufferEmpty;
        private bool _interruptFlag;

        // Rate table for DMC timer periods (in APU cycles)
        private static readonly int[] RateTable =
        [
            428, 380, 340, 320, 286, 254, 226, 214, 190, 160, 142, 128, 106, 84, 72, 54
        ];

        // Delegate for memory reads - the APU will need to provide this
        public delegate byte MemoryReadDelegate(ushort address);
        public MemoryReadDelegate? MemoryRead { get; set; }

        // Event for DMC interrupts
        public event EventHandler? DmcInterrupt;

        public DMC(float sampleRate = 44100f) : base(sampleRate)
        {
            // Initialize state
            _outputLevel = 0;
            _silenceFlag = true;
            _bitsRemaining = 0;
            _bytesRemaining = 0;
            _sampleBufferEmpty = true;
        }

        public override void WriteRegister(int register, byte value)
        {
            switch (register & 3)
            {
                case 0: // $4010 - IL-- RRRR (IRQ enable, Loop, Rate)
                    _control = value;

                    // Clear interrupt flag if IRQ is disabled
                    if ((_control & 0x80) == 0)
                    {
                        _interruptFlag = false;
                    }

                    // Update timer period from rate bits
                    int rateIndex = value & 0x0F;
                    _timerPeriod = RateTable[rateIndex];
                    break;

                case 1: // $4011 - -DDD DDDD (Direct load)
                    _directLoad = value;
                    _outputLevel = (byte)(value & 0x7F); // 7-bit value
                    break;

                case 2: // $4012 - AAAA AAAA (Sample address)
                    _sampleAddress = value;
                    break;

                case 3: // $4013 - LLLL LLLL (Sample length)
                    _sampleLength = value;
                    break;
            }
        }

        /// <summary>
        /// Starts DMC sample playback. Called when bit 4 of $4015 is set.
        /// </summary>
        public void StartSample()
        {
            // Calculate starting address: $C000 + (value * $40)
            _currentAddress = (ushort)(0xC000 + (_sampleAddress * 0x40));

            // Calculate sample length: (value * $10) + 1
            _bytesRemaining = (_sampleLength * 0x10) + 1;

            // Fill sample buffer if empty
            if (_sampleBufferEmpty && _bytesRemaining > 0)
            {
                FillSampleBuffer();
            }
        }

        /// <summary>
        /// Stops DMC sample playback. Called when bit 4 of $4015 is cleared.
        /// </summary>
        public void StopSample()
        {
            _bytesRemaining = 0;
        }

        /// <summary>
        /// Checks if the DMC channel is currently active.
        /// </summary>
        public override bool IsActive()
        {
            return _bytesRemaining > 0;
        }

        /// <summary>
        /// Gets the current interrupt flag state.
        /// </summary>
        public bool GetInterruptFlag()
        {
            return _interruptFlag;
        }

        /// <summary>
        /// Clears the interrupt flag. Called when reading $4015.
        /// </summary>
        public void ClearInterruptFlag()
        {
            _interruptFlag = false;
        }

        /// <summary>
        /// Resets the channel to its initial state.
        /// </summary>
        public override void Reset()
        {
            _control = 0;
            _directLoad = 0;
            _sampleAddress = 0;
            _sampleLength = 0;
            _timer = 0;
            _timerPeriod = RateTable[0];
            _outputLevel = 0;
            _silenceFlag = true;
            _shifterRegister = 0;
            _bitsRemaining = 0;
            _bytesRemaining = 0;
            _currentAddress = 0;
            _sampleBuffer = 0;
            _sampleBufferEmpty = true;
            _interruptFlag = false;
        }

        /// <summary>
        /// Fills the sample buffer with the next byte from memory.
        /// </summary>
        private void FillSampleBuffer()
        {
            if (MemoryRead != null && _bytesRemaining > 0)
            {
                _sampleBuffer = MemoryRead(_currentAddress);
                _sampleBufferEmpty = false;

                _currentAddress++;
                if (_currentAddress == 0) // Handle address wraparound
                {
                    _currentAddress = 0x8000;
                }

                _bytesRemaining--;

                // Check if sample finished
                if (_bytesRemaining == 0)
                {
                    // Check if loop flag is set
                    if ((_control & 0x40) != 0)
                    {
                        // Restart sample
                        StartSample();
                    }
                    else
                    {
                        // Generate interrupt if enabled
                        if ((_control & 0x80) != 0)
                        {
                            _interruptFlag = true;
                            DmcInterrupt?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clocks the DMC output unit.
        /// </summary>
        private void ClockOutput()
        {
            if (!_silenceFlag)
            {
                // Get the current bit from the shift register
                int bit = _shifterRegister & 1;
                _shifterRegister >>= 1;

                // Modify output level based on bit
                if (bit == 1)
                {
                    if (_outputLevel <= 125)
                        _outputLevel += 2;
                }
                else
                {
                    if (_outputLevel >= 2)
                        _outputLevel -= 2;
                }
            }

            _bitsRemaining--;

            // Check if we need to load a new byte
            if (_bitsRemaining == 0)
            {
                _bitsRemaining = 8;

                if (!_sampleBufferEmpty)
                {
                    _silenceFlag = false;
                    _shifterRegister = _sampleBuffer;
                    _sampleBufferEmpty = true;

                    // Try to fill the buffer again
                    FillSampleBuffer();
                }
                else
                {
                    _silenceFlag = true;
                }
            }
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Convert sample rate to APU frequency
                float apuCyclesPerSample = GetApuCyclesPerSample();

                // Timer clocking
                _timer -= (int)apuCyclesPerSample;
                if (_timer <= 0)
                {
                    _timer += _timerPeriod;
                    ClockOutput();
                }

                // Generate sample
                // DMC output is a 7-bit value (0-127), convert to float range
                float sample = (_outputLevel - 64f) / 64f * 0.5f; // Center around 0 and reduce volume

                buffer[offset + i] = sample;
            }

            return count;
        }
    }
}