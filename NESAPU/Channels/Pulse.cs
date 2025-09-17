using NAudio.Wave;

namespace NESAPU.Channels
{
    public class Pulse : ISampleProvider
    {
        private readonly float _sampleRate;

        // Pulse channel registers
        private byte _dutyControl;      // $4000/$4004 - DDLC VVVV
        private byte _sweepControl;     // $4001/$4005 - EPPP NSSS  
        private byte _timerLow;         // $4002/$4006 - TTTT TTTT
        private byte _lengthTimerHigh;  // $4003/$4007 - LLLL LTTT

        // Internal channel state
        private int _timer;
        private int _timerPeriod;
        private int _dutyCounter;
        private int _lengthCounter;
        private int _envelopeCounter;
        private int _envelopeVolume;
        private bool _envelopeStart;
        private int _sweepCounter;
        private bool _sweepReload;
        private int _targetPeriod;

        // Duty cycle patterns for the NES APU
        private static readonly byte[][] DutyPatterns =
        [
            [0, 1, 0, 0, 0, 0, 0, 0], // 12.5%
            [0, 1, 1, 0, 0, 0, 0, 0], // 25%
            [0, 1, 1, 1, 1, 0, 0, 0], // 50%
            [1, 0, 0, 1, 1, 1, 1, 1]  // 25% negated
        ];

        public WaveFormat WaveFormat { get; }

        public Pulse(float sampleRate = 44100f)
        {
            _sampleRate = sampleRate;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat((int)sampleRate, 1);

            // Initialize registers and state
            _lengthCounter = 0;
            _envelopeVolume = 15;
        }

        public void WriteRegister(int register, byte value)
        {
            switch (register & 3)
            {
                case 0: // DDLC VVVV - Duty, Length halt, Constant volume, Volume/Envelope
                    _dutyControl = value;
                    _envelopeStart = true;
                    break;

                case 1: // EPPP NSSS - Sweep enable, Period, Negate, Shift
                    _sweepControl = value;
                    _sweepReload = true;
                    break;

                case 2: // TTTT TTTT - Timer low
                    _timerLow = value;
                    UpdateTimerPeriod();
                    break;

                case 3: // LLLL LTTT - Length load, Timer high
                    _lengthTimerHigh = value;
                    UpdateTimerPeriod();

                    // Cargar length counter
                    int lengthLoad = (value >> 3) & 0x1F;
                    _lengthCounter = GetLengthValue(lengthLoad);

                    // Reiniciar fase del duty cycle
                    _dutyCounter = 0;
                    _envelopeStart = true;
                    break;
            }
        }
        
        /// <summary>
        /// Sets the length counter to the specified value. Used by APU when channels are enabled/disabled.
        /// </summary>
        /// <param name="value">The new length counter value (0 to disable the channel)</param>
        public void SetLengthCounter(int value)
        {
            _lengthCounter = value;
        }

        /// <summary>
        /// Gets the current length counter value. Used by APU for status register reads.
        /// </summary>
        /// <returns>Current length counter value</returns>
        public int GetLengthCounter()
        {
            return _lengthCounter;
        }

        /// <summary>
        /// Checks if the channel is currently active (length counter > 0).
        /// Used by APU for status register bit determination.
        /// </summary>
        /// <returns>True if the channel is active, false otherwise</returns>
        public bool IsActive()
        {
            return _lengthCounter > 0;
        }

        /// <summary>
        /// Resets the channel to its initial state. Called when APU is reset.
        /// </summary>
        public void Reset()
        {
            _lengthCounter = 0;
            _envelopeVolume = 15;
            _envelopeStart = false;
            _envelopeCounter = 0;
            _sweepCounter = 0;
            _sweepReload = false;
            _timer = 0;
            _timerPeriod = 0;
            _dutyCounter = 0;
            _targetPeriod = 0;
            
            // Reset registers
            _dutyControl = 0;
            _sweepControl = 0;
            _timerLow = 0;
            _lengthTimerHigh = 0;
        }

        private void UpdateTimerPeriod()
        {
            _timerPeriod = _timerLow | ((_lengthTimerHigh & 0x07) << 8);
            CalculateTargetPeriod();
        }

        private void CalculateTargetPeriod()
        {
            int shiftAmount = _sweepControl & 0x07;
            _targetPeriod = _timerPeriod >> shiftAmount;

            if ((_sweepControl & 0x08) != 0) // Negate flag
            {
                _targetPeriod = _timerPeriod - _targetPeriod;
            }
            else
            {
                _targetPeriod = _timerPeriod + _targetPeriod;
            }
        }

        // Length counter value table for NES
        private static readonly byte[] LengthTable =
        [
            10, 254, 20,  2, 40,  4, 80,  6, 160,  8, 60, 10, 14, 12, 26, 14,
            12,  16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30
        ];

        private static int GetLengthValue(int index)
        {
            return LengthTable[index];
        }

        // Envelope clock (call at 240Hz)
        public void ClockEnvelope()
        {
            if (_envelopeStart)
            {
                _envelopeStart = false;
                _envelopeVolume = 15;
                _envelopeCounter = _dutyControl & 0x0F;
            }
            else if (_envelopeCounter > 0)
            {
                _envelopeCounter--;
            }
            else
            {
                _envelopeCounter = _dutyControl & 0x0F;
                if (_envelopeVolume > 0)
                {
                    _envelopeVolume--;
                }
                else if ((_dutyControl & 0x20) != 0) // Length halt/Envelope loop
                {
                    _envelopeVolume = 15;
                }
            }
        }

        // Length counter clock (call at 120Hz)
        public void ClockLength()
        {
            if ((_dutyControl & 0x20) == 0 && _lengthCounter > 0) // Length halt flag
            {
                _lengthCounter--;
            }
        }

        // Sweep clock (call at 120Hz)
        public void ClockSweep()
        {
            if (_sweepCounter == 0 && (_sweepControl & 0x80) != 0) // Sweep enabled
            {
                if (_timerPeriod >= 8 && _targetPeriod <= 0x7FF)
                {
                    _timerPeriod = _targetPeriod;
                    UpdateTimerPeriod();
                }
            }

            if (_sweepCounter == 0 || _sweepReload)
            {
                _sweepCounter = (_sweepControl >> 4) & 0x07;
                _sweepReload = false;
            }
            else
            {
                _sweepCounter--;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Convert sample rate to APU frequency (approximately 1.789773 MHz)
                float apuCyclesPerSample = 1789773f / (2f * _sampleRate);

                // Timer clocking
                _timer -= (int)apuCyclesPerSample;
                if (_timer <= 0)
                {
                    _timer += _timerPeriod + 1;
                    _dutyCounter = (_dutyCounter + 1) % 8;
                }

                // Generate sample
                float sample = 0f;

                // Generate sound only if the channel is active
                if (_lengthCounter > 0 && _timerPeriod >= 8 && _timerPeriod <= 0x7FF)
                {
                    // Get duty cycle pattern
                    int dutySelect = (_dutyControl >> 6) & 0x03;
                    byte dutyOutput = DutyPatterns[dutySelect][_dutyCounter];

                    if (dutyOutput == 1)
                    {
                        // Volume: use envelope or constant volume
                        int volume = ((_dutyControl & 0x10) != 0) ?
                                     (_dutyControl & 0x0F) : _envelopeVolume;

                        // Convert to float audio range (-1.0 to 1.0)
                        sample = (volume / 15f) * 0.5f; // Reduce volume for mixing
                    }
                }

                buffer[offset + i] = sample;
            }

            return count;
        }
    }
}