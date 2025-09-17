using NAudio.Wave;

namespace NESAPU.Channels
{
    public class Triangle : ISampleProvider
    {
        private readonly float _sampleRate;

        // Triangle channel registers
        private byte _linearControl;    // $4008 - CRRR RRRR (Control, Reload)
        private byte _unused;           // $4009 - Unused
        private byte _timerLow;         // $400A - TTTT TTTT (Timer low)
        private byte _lengthTimerHigh;  // $400B - LLLL LTTT (Length load, Timer high)

        // Internal state
        private int _timer;
        private int _timerPeriod;
        private int _sequenceCounter;   // 0-31 for triangle wave sequence
        private int _lengthCounter;
        private int _linearCounter;
        private int _linearCounterReload;
        private bool _linearCounterReloadFlag;
        private float _lastSample;      // For simple low-pass filtering

        // Triangle wave sequence (32 steps)
        // Produces values 0, 1, 2, ..., 15, 15, 14, 13, ..., 1, 0
        private static readonly byte[] TriangleSequence =
        [
            15, 14, 13, 12, 11, 10, 9,  8,  7,  6,  5,  4,  3,  2,  1,  0,
            0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15
        ];

        public WaveFormat WaveFormat { get; }

        public Triangle(float sampleRate = 44100f)
        {
            _sampleRate = sampleRate;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat((int)sampleRate, 1);

            // Initialize state
            _lengthCounter = 0;
            _linearCounter = 0;
            _sequenceCounter = 0;
        }

        public void WriteRegister(int register, byte value)
        {
            switch (register & 3)
            {
                case 0: // $4008 - CRRR RRRR (Linear counter control, reload value)
                    _linearControl = value;
                    _linearCounterReload = value & 0x7F; // Bottom 7 bits
                    break;

                case 1: // $4009 - Unused
                    _unused = value;
                    break;

                case 2: // $400A - TTTT TTTT (Timer low)
                    _timerLow = value;
                    UpdateTimerPeriod();
                    break;

                case 3: // $400B - LLLL LTTT (Length counter load, Timer high)
                    _lengthTimerHigh = value;
                    UpdateTimerPeriod();

                    // Load length counter
                    int lengthLoad = (value >> 3) & 0x1F;
                    _lengthCounter = GetLengthValue(lengthLoad);

                    // Set linear counter reload flag
                    _linearCounterReloadFlag = true;
                    break;
            }
        }

        private void UpdateTimerPeriod()
        {
            _timerPeriod = _timerLow | ((_lengthTimerHigh & 0x07) << 8);
        }

        // Same length table as pulse channels
        private static readonly byte[] LengthTable =
        [
            10, 254, 20,  2, 40,  4, 80,  6, 160,  8, 60, 10, 14, 12, 26, 14,
            12,  16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30
        ];

        private static int GetLengthValue(int index)
        {
            return LengthTable[index];
        }

        /// <summary>
        /// Sets the length counter to the specified value. Used by APU when channel is enabled/disabled.
        /// </summary>
        public void SetLengthCounter(int value)
        {
            _lengthCounter = value;
        }

        /// <summary>
        /// Gets the current length counter value.
        /// </summary>
        public int GetLengthCounter()
        {
            return _lengthCounter;
        }

        /// <summary>
        /// Checks if the channel is currently active.
        /// </summary>
        public bool IsActive()
        {
            return _lengthCounter > 0;
        }

        /// <summary>
        /// Resets the channel to its initial state.
        /// </summary>
        public void Reset()
        {
            _lengthCounter = 0;
            _linearCounter = 0;
            _linearCounterReload = 0;
            _linearCounterReloadFlag = false;
            _timer = 0;
            _timerPeriod = 0;
            _sequenceCounter = 0;
            _lastSample = 0f;

            // Reset registers
            _linearControl = 0;
            _unused = 0;
            _timerLow = 0;
            _lengthTimerHigh = 0;
        }

        /// <summary>
        /// Clock the linear counter (called at 240Hz).
        /// </summary>
        public void ClockLinearCounter()
        {
            if (_linearCounterReloadFlag)
            {
                _linearCounter = _linearCounterReload;
            }
            else if (_linearCounter > 0)
            {
                _linearCounter--;
            }

            // Clear reload flag if control flag is clear
            if ((_linearControl & 0x80) == 0) // Control flag (bit 7)
            {
                _linearCounterReloadFlag = false;
            }
        }

        /// <summary>
        /// Clock the length counter (called at 120Hz).
        /// </summary>
        public void ClockLength()
        {
            // Length counter is clocked if control flag is clear and length > 0
            if ((_linearControl & 0x80) == 0 && _lengthCounter > 0) // Control flag also acts as length halt
            {
                _lengthCounter--;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Convert sample rate to APU frequency
                float apuCyclesPerSample = 1789773f / (32f * _sampleRate); // APU = CPU/2

                // Timer clocking
                _timer -= (int)apuCyclesPerSample;
                if (_timer <= 0)
                {
                    _timer += _timerPeriod + 1;

                    // Triangle channel clocks the sequencer if both counters are non-zero
                    // AND timer period is not too low (ultrasonic frequencies are silenced)
                    if (_lengthCounter > 0 && _linearCounter > 0)
                    {
                        _sequenceCounter = (_sequenceCounter + 1) % 32;
                    }
                }

                // Generate sample
                float sample = 0f;

                // Output triangle wave if both length and linear counters are active
                // and timer period is in valid range
                if (_timerPeriod >= 2)
                {
                    // Get current step in triangle sequence
                    byte triangleStep = TriangleSequence[_sequenceCounter];

                    // Convert to audio range (-1.0 to 1.0)
                    // Triangle channel doesn't have volume control, always full amplitude
                    sample = (triangleStep / 15.0f) * 0.5f - 0.25f; // Center around 0 and reduce volume
                    _lastSample = sample;
                }
                else
                {
                    sample = _lastSample;
                }

                buffer[offset + i] = sample;
            }

            return count;
        }
    }
}