using NAudio.Wave;

namespace NESAPU.Channels
{
    public class Pulse : Channel
    {
        private readonly bool _isPulse1;

        // Pulse channel registers
        private byte _dutyControl;      // $4000/$4004 - DDLC VVVV
        private byte _sweepControl;     // $4001/$4005 - EPPP NSSS  
        private byte _timerLow;         // $4002/$4006 - TTTT TTTT
        private byte _lengthTimerHigh;  // $4003/$4007 - LLLL LTTT

        // Internal channel state
        private int _dutyCounter;
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

        public Pulse(float sampleRate = 44100f, bool isPulse1 = true) : base(sampleRate)
        {
            _isPulse1 = isPulse1;
        }

        public override void WriteRegister(int register, byte value)
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

                    // Load length counter
                    int lengthLoad = (value >> 3) & 0x1F;
                    _lengthCounter = GetLengthValue(lengthLoad);

                    // Reset duty cycle phase
                    _dutyCounter = 0;
                    _envelopeStart = true;
                    break;
            }
        }

        /// <summary>
        /// Resets the channel to its initial state. Called when APU is reset.
        /// </summary>
        public override void Reset()
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
            int changeAmount = _timerPeriod >> shiftAmount;

            if ((_sweepControl & 0x08) != 0) // Negate flag
            {
                // Pulse 1 uses one's complement, Pulse 2 uses two's complement
                _targetPeriod = _timerPeriod - changeAmount - (_isPulse1 ? 1 : 0);
            }
            else
            {
                _targetPeriod = _timerPeriod + changeAmount;
            }
        }

        // Envelope clock (call at 240Hz) - now uses base class functionality
        public void ClockEnvelope()
        {
            ClockEnvelope(_dutyControl);
        }

        // Length counter clock (call at 120Hz)
        public override void ClockLength()
        {
            if ((_dutyControl & 0x20) == 0 && _lengthCounter > 0) // Length halt flag
            {
                _lengthCounter--;
            }
        }

        // Sweep clock (call at 120Hz)
        public void ClockSweep()
        {
            // Update target period calculation always
            CalculateTargetPeriod();
            
            // Clock the sweep divider
            if (_sweepCounter == 0 || _sweepReload)
            {
                _sweepCounter = (_sweepControl >> 4) & 0x07;
                _sweepReload = false;
                
                // Apply sweep if enabled and conditions are met
                if ((_sweepControl & 0x80) != 0 && (_sweepControl & 0x07) != 0) // Enabled and shift > 0
                {
                    if (_timerPeriod >= 8 && _targetPeriod <= 0x7FF)
                    {
                        _timerPeriod = _targetPeriod;
                        UpdateTimerPeriod();
                    }
                }
            }
            else if (_sweepCounter > 0)
            {
                _sweepCounter--;
            }
        }

        public override int Read(float[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Convert sample rate to APU frequency (approximately 1.789773 MHz)
                float apuCyclesPerSample = GetApuCyclesPerSample();

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
                if (_lengthCounter > 0 && _timerPeriod >= 8 && _timerPeriod <= 0x7FF && 
                    (_targetPeriod <= 0x7FF || (_sweepControl & 0x80) == 0))
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