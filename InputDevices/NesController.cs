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
    private NesControllerButtons _currentButtons = NesControllerButtons.None;
    private byte _shiftRegister;
    private bool _strobing;

    public NesController()
    {
        _shiftRegister = 0;
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
    /// Called when CPU writes to $4016 or $4017 (strobe)
    /// </summary>
    public void WriteStrobe(byte value)
    {
        _strobing = (value & 0x01) != 0;
        _shiftRegister = (byte)_currentButtons;
    }

    public byte ReadButtonState()
    {
        byte result = (byte)(_shiftRegister & 1);
        
        if (!_strobing)
        {
            // Shift right for next read
            _shiftRegister >>= 1;
        }

        return (byte)(result | 0x40); // Bit 6 is always high
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
