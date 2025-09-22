using NAudio.Wave;

namespace NESAPU.Channels
{
    /// <summary>
    /// Base class for NES APU audio channels containing common functionality.
    /// </summary>
    public abstract class Channel : ISampleProvider
    {
        protected readonly float _sampleRate;

        // Common timer functionality
        protected int _timer;
        protected int _timerPeriod;

        // Length counter functionality (used by most channels)
        protected int _lengthCounter;

        // Envelope functionality (used by Pulse and Noise channels)
        protected int _envelopeCounter;
        protected int _envelopeVolume;
        protected bool _envelopeStart;

        // Common length counter value table for NES
        protected static readonly byte[] LengthTable =
        [
            10, 254, 20,  2, 40,  4, 80,  6, 160,  8, 60, 10, 14, 12, 26, 14,
            12,  16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30
        ];

        public WaveFormat WaveFormat { get; }

        protected Channel(float sampleRate = 44100f)
        {
            _sampleRate = sampleRate;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat((int)sampleRate, 1);
            
            // Initialize common state
            _lengthCounter = 0;
            _envelopeVolume = 15;
        }

        /// <summary>
        /// Writes a value to the specified register of the channel.
        /// </summary>
        /// <param name="register">Register index (0-3 for most channels)</param>
        /// <param name="value">Value to write</param>
        public abstract void WriteRegister(int register, byte value);

        /// <summary>
        /// Sets the length counter to the specified value. Used by APU when channel is enabled/disabled.
        /// </summary>
        /// <param name="value">The new length counter value (0 to disable the channel)</param>
        public virtual void SetLengthCounter(int value)
        {
            _lengthCounter = value;
        }

        /// <summary>
        /// Gets the current length counter value. Used by APU for status register reads.
        /// </summary>
        /// <returns>Current length counter value</returns>
        public virtual int GetLengthCounter()
        {
            return _lengthCounter;
        }

        /// <summary>
        /// Checks if the channel is currently active.
        /// </summary>
        /// <returns>True if the channel is active, false otherwise</returns>
        public virtual bool IsActive()
        {
            return _lengthCounter > 0;
        }

        /// <summary>
        /// Resets the channel to its initial state. Called when APU is reset.
        /// </summary>
        public abstract void Reset();

        /// <summary>
        /// Clocks the length counter (called at 120Hz). 
        /// Override this method in derived classes to implement specific length counter behavior.
        /// </summary>
        public virtual void ClockLength()
        {
            // Default implementation - can be overridden by derived classes
            if (_lengthCounter > 0)
            {
                _lengthCounter--;
            }
        }

        /// <summary>
        /// Clocks the envelope generator (called at 240Hz).
        /// This is used by Pulse and Noise channels. Override in derived classes as needed.
        /// </summary>
        /// <param name="envelopeControl">The envelope control register value</param>
        public virtual void ClockEnvelope(byte envelopeControl)
        {
            if (_envelopeStart)
            {
                _envelopeStart = false;
                _envelopeVolume = 15;
                _envelopeCounter = envelopeControl & 0x0F;
            }
            else if (_envelopeCounter > 0)
            {
                _envelopeCounter--;
            }
            else
            {
                _envelopeCounter = envelopeControl & 0x0F;
                if (_envelopeVolume > 0)
                {
                    _envelopeVolume--;
                }
                else if ((envelopeControl & 0x20) != 0) // Length halt/Envelope loop
                {
                    _envelopeVolume = 15;
                }
            }
        }

        /// <summary>
        /// Gets the length value from the length table using the specified index.
        /// </summary>
        /// <param name="index">Index into the length table (0-31)</param>
        /// <returns>Length value from the table</returns>
        protected static int GetLengthValue(int index)
        {
            return LengthTable[index];
        }

        /// <summary>
        /// Calculates APU cycles per sample for timing calculations.
        /// </summary>
        /// <returns>Number of APU cycles per audio sample</returns>
        protected float GetApuCyclesPerSample()
        {
            return 1789773f / (2f * _sampleRate); // APU = CPU/2
        }

        /// <summary>
        /// ISampleProvider implementation - generates audio samples for this channel.
        /// </summary>
        /// <param name="buffer">Audio buffer to fill</param>
        /// <param name="offset">Offset in the buffer to start filling</param>
        /// <param name="count">Number of samples to generate</param>
        /// <returns>Number of samples actually generated</returns>
        public abstract int Read(float[] buffer, int offset, int count);
    }
}