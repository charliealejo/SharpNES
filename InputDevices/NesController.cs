using Ricoh6502;

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

    private bool _strobe;
    private bool _latch;
    private int _currentButtonIndex;

    private byte[] _cpuMemory;

    public NesController(byte[] cpuMemory)
    {
        _cpuMemory = cpuMemory ?? throw new ArgumentNullException(nameof(cpuMemory));
        _strobe = false;
        _latch = false;
        _currentButtonIndex = 0;
    }

    /// <summary>
    /// Gets or sets the currently pressed buttons
    /// </summary>
    public NesControllerButtons CurrentButtons
    {
        get => _currentButtons;
        set => _currentButtons = value;
    }

    public void Strobe()
    {
        _strobe = true;
    }

    public void LatchState()
    {
        if (_strobe)
        {
            _strobe = false;
            _latch = true;
        }
    }

    public void Clock()
    {
        if (_latch)
        {
            _cpuMemory[Controller1Address] = (byte)(((byte)_currentButtons & (1 << _currentButtonIndex)) != 0 ? 1 : 0);
            _currentButtonIndex++;
        
            if (_currentButtonIndex >= 8)
            {
                _currentButtonIndex = 0;
                _latch = false;
            }
        }
        else
        {
            _cpuMemory[Controller1Address] = 1;
        }
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

    /// <summary>
    /// Checks if a specific button is currently pressed
    /// </summary>
    /// <param name="button">The button to check</param>
    /// <returns>True if the button is pressed, false otherwise</returns>
    public bool IsButtonPressed(NesControllerButtons button)
    {
        return (_currentButtons & button) != 0;
    }

    /// <summary>
    /// Clears all button states
    /// </summary>
    public void ClearAllButtons()
    {
        _currentButtons = NesControllerButtons.None;
    }
}
