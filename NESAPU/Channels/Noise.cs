using NAudio.Wave;

namespace NESAPU.Channels
{
    public class Noise : ISampleProvider
    {
        private readonly float _sampleRate;

        // Noise channel registers
        private byte _envelopeControl;  // $400C - --LC VVVV (Length halt, Constant volume, Volume/Envelope)
        private byte _unused;           // $400D - Unused
        private byte _periodControl;    // $400E - M--- PPPP (Mode, Period)
        private byte _lengthLoad;       // $400F - LLLL L--- (Length counter load)

        // Internal state
        private int _timer;
        private int _timerPeriod;
        private int _lengthCounter;
        private int _envelopeCounter;
        private int _envelopeVolume;
        private bool _envelopeStart;
        private ushort _shiftRegister;  // 15-bit Linear Feedback Shift Register
        private bool _modeFlag;         // false = 15-bit mode, true = 6-bit mode

        // Noise timer period table (in APU cycles)
        private static readonly int[] NoisePeriodTable =
        [
            4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068
        ];

        // Same length table as other channels
        private static readonly byte[] LengthTable =
        [
            10, 254, 20,  2, 40,  4, 80,  6, 160,  8, 60, 10, 14, 12, 26, 14,
            12,  16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30
        ];

        public WaveFormat WaveFormat { get; }

        public Noise(float sampleRate = 44100f)
        {
            _sampleRate = sampleRate;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat((int)sampleRate, 1);

            // Initialize state
            _lengthCounter = 0;
            _envelopeVolume = 15;
            _shiftRegister = 1; // Start with non-zero value
        }

        public void WriteRegister(int register, byte value)
        {
            switch (register & 3)
            {
                case 0: // $400C - --LC VVVV (Length halt, Constant volume, Volume/Envelope)
                    _envelopeControl = value;
                    _envelopeStart = true;
                    break;

                case 1: // $400D - Unused
                    _unused = value;
                    break;

                case 2: // $400E - M--- PPPP (Mode flag, Period index)
                    _periodControl = value;
                    _modeFlag = (value & 0x80) != 0; // Bit 7: Mode flag
                    int periodIndex = value & 0x0F;  // Bottom 4 bits: Period index
                    _timerPeriod = NoisePeriodTable[periodIndex];
                    break;

                case 3: // $400F - LLLL L--- (Length counter load)
                    _lengthLoad = value;

                    // Load length counter
                    int lengthIndex = (value >> 3) & 0x1F;
                    _lengthCounter = LengthTable[lengthIndex];

                    // Restart envelope
                    _envelopeStart = true;
                    break;
            }
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
            _envelopeVolume = 15;
            _envelopeStart = false;
            _envelopeCounter = 0;
            _timer = 0;
            _timerPeriod = NoisePeriodTable[0];
            _shiftRegister = 1;
            _modeFlag = false;

            // Reset registers
            _envelopeControl = 0;
            _unused = 0;
            _periodControl = 0;
            _lengthLoad = 0;
        }

        /// <summary>
        /// Clock the envelope (called at 240Hz).
        /// </summary>
        public void ClockEnvelope()
        {
            if (_envelopeStart)
            {
                _envelopeStart = false;
                _envelopeVolume = 15;
                _envelopeCounter = _envelopeControl & 0x0F;
            }
            else if (_envelopeCounter > 0)
            {
                _envelopeCounter--;
            }
            else
            {
                _envelopeCounter = _envelopeControl & 0x0F;
                if (_envelopeVolume > 0)
                {
                    _envelopeVolume--;
                }
                else if ((_envelopeControl & 0x20) != 0) // Length halt/Envelope loop
                {
                    _envelopeVolume = 15;
                }
            }
        }

        /// <summary>
        /// Clock the length counter (called at 120Hz).
        /// </summary>
        public void ClockLength()
        {
            if ((_envelopeControl & 0x20) == 0 && _lengthCounter > 0) // Length halt flag
            {
                _lengthCounter--;
            }
        }

        /// <summary>
        /// Clock the Linear Feedback Shift Register to generate the next noise bit.
        /// </summary>
        private void ClockShiftRegister()
        {
            // Get feedback bit
            int feedbackBit;
            if (_modeFlag) // 6-bit mode
            {
                feedbackBit = ((_shiftRegister & 1) ^ ((_shiftRegister >> 6) & 1));
            }
            else // 15-bit mode (normal)
            {
                feedbackBit = ((_shiftRegister & 1) ^ ((_shiftRegister >> 1) & 1));
            }

            // Shift register right and insert feedback bit at the top
            _shiftRegister >>= 1;
            _shiftRegister |= (ushort)(feedbackBit << 14);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Convert sample rate to APU frequency
                float apuCyclesPerSample = 1789773f / (2f * _sampleRate); // APU = CPU/2

                // Timer clocking
                _timer -= (int)apuCyclesPerSample;
                if (_timer <= 0)
                {
                    _timer += _timerPeriod;
                    ClockShiftRegister();
                }

                // Generate sample
                float sample = 0f;

                // Output noise if channel is active and shift register bit 0 is clear
                if (_lengthCounter > 0 && (_shiftRegister & 1) == 0)
                {
                    // Volume: use envelope or constant volume
                    int volume = ((_envelopeControl & 0x10) != 0) ?
                                 (_envelopeControl & 0x0F) : _envelopeVolume;

                    // Convert to audio range
                    sample = (volume / 15.0f) * 0.5f; // Reduce volume for mixing
                }

                buffer[offset + i] = sample;
            }

            return count;
        }
    }
}
