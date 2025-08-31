namespace InputDevices;

/// <summary>
/// Represents the buttons on a standard NES controller
/// </summary>
[Flags]
public enum NesControllerButtons : byte
{
    None = 0,
    A = 1 << 0,
    B = 1 << 1,
    Select = 1 << 2,
    Start = 1 << 3,
    Up = 1 << 4,
    Down = 1 << 5,
    Left = 1 << 6,
    Right = 1 << 7
}

/// <summary>
/// Represents a standard NES controller
/// </summary>
public class NesController
{
    private const ushort Controller1Address = 0x4016;
    private const ushort Controller2Address = 0x4017;

    private NesControllerButtons _currentButtons = NesControllerButtons.None;
    private byte _shiftRegister;
    private int _readCount;

    public NesController()
    {
        _shiftRegister = 0;
        _readCount = 0;
    }

    /// <summary>
    /// Gets or sets the currently pressed buttons
    /// </summary>
    public NesControllerButtons CurrentButtons
    {
        get => _currentButtons;
        set => _currentButtons = value;
    }

    /// <summary>
    /// Called when CPU writes to $4016 (strobe)
    /// </summary>
    /// <param name="value">Value written (1 = start strobe, 0 = end strobe and latch)</param>
    public void WriteStrobe(byte value)
    {
        if (value == 0) // End of strobe - latch current button state
        {
            _shiftRegister = (byte)_currentButtons;
            _readCount = 0;
        }
    }

    public byte ReadButtonState()
    {
        byte result;
        
        if (_readCount < 8)
        {
            // Return LSB and shift right for next read
            result = (byte)(_shiftRegister & 1);
            _shiftRegister >>= 1;
            _readCount++;
        }
        else
        {
            // After 8 reads, return 1 (standard behavior)
            result = 1;
        }
        
        return result;
    }

    /// <summary>
    /// Sets the state of a specific button
    /// </summary>
    /// <param name="button">The button to set</param>
    /// <param name="pressed">Whether the button is pressed</param>
    public void SetButtonState(NesControllerButtons button, bool pressed)
    {
        if (pressed)
        {
            _currentButtons |= button;
        }
        else
        {
            _currentButtons &= ~button;
        }
    }
}
