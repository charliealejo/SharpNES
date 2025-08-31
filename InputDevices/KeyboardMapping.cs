using System.Collections.Concurrent;

namespace InputDevices;

public class KeyboardMapping : IDisposable
{
    private Dictionary<ConsoleKey, NesControllerButtons> _player1KeyMap;
    private Dictionary<ConsoleKey, NesControllerButtons> _player2KeyMap;
    private readonly ConcurrentDictionary<ConsoleKey, bool> _pressedKeys;
    private readonly NesController _controller;
    private readonly Thread _inputThread;
    private volatile bool _isRunning;

    public KeyboardMapping(NesController controller)
    {
        _controller = controller;
        _pressedKeys = new ConcurrentDictionary<ConsoleKey, bool>();
        
        _player1KeyMap = new Dictionary<ConsoleKey, NesControllerButtons>
        {
            { ConsoleKey.M, NesControllerButtons.A },
            { ConsoleKey.N, NesControllerButtons.B },
            { ConsoleKey.Enter, NesControllerButtons.Start },
            { ConsoleKey.Spacebar, NesControllerButtons.Select },
            { ConsoleKey.W, NesControllerButtons.Up },
            { ConsoleKey.S, NesControllerButtons.Down },
            { ConsoleKey.A, NesControllerButtons.Left },
            { ConsoleKey.D, NesControllerButtons.Right }
        };
        
        _player2KeyMap = new Dictionary<ConsoleKey, NesControllerButtons>
        {
            { ConsoleKey.Decimal, NesControllerButtons.A },
            { ConsoleKey.NumPad0, NesControllerButtons.B },
            { ConsoleKey.Enter, NesControllerButtons.Start },
            { ConsoleKey.Spacebar, NesControllerButtons.Select },
            { ConsoleKey.UpArrow, NesControllerButtons.Up },
            { ConsoleKey.DownArrow, NesControllerButtons.Down },
            { ConsoleKey.LeftArrow, NesControllerButtons.Left },
            { ConsoleKey.RightArrow, NesControllerButtons.Right }
        };

        // Start keyboard monitoring
        _isRunning = true;
        _inputThread = new Thread(MonitorKeyboard) { IsBackground = true };
        _inputThread.Start();
    }

    private void MonitorKeyboard()
    {
        while (_isRunning)
        {
            if (Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey(true);
                var key = keyInfo.Key;
                
                // Handle key press
                if (!_pressedKeys.ContainsKey(key))
                {
                    _pressedKeys[key] = true;
                    OnKeyPressed(key);
                }
            }
            else
            {
                // Check for key releases (simple approach - clear all keys when no input)
                if (_pressedKeys.Count > 0)
                {
                    var keysToRemove = _pressedKeys.Keys.ToList();
                    foreach (var key in keysToRemove)
                    {
                        _pressedKeys.TryRemove(key, out _);
                        OnKeyReleased(key);
                    }
                }
            }
            
            Thread.Sleep(16); // ~60 FPS polling rate
        }
    }

    private void OnKeyPressed(ConsoleKey key)
    {
        if (_player1KeyMap.TryGetValue(key, out var button))
        {
            _controller.SetButtonState(button, true);
        }
        else if (_player2KeyMap.TryGetValue(key, out button))
        {
            _controller.SetButtonState(button, true);
        }
    }

    private void OnKeyReleased(ConsoleKey key)
    {
        if (_player1KeyMap.TryGetValue(key, out var button))
        {
            _controller.SetButtonState(button, false);
        }
        else if (_player2KeyMap.TryGetValue(key, out button))
        {
            _controller.SetButtonState(button, false);
        }
    }

    public void ApplyKeyboardState(List<ConsoleKey> pressedKeys)
    {
        _controller.ClearAllButtons();

        // Map keyboard keys to NES controller buttons
        foreach (var key in pressedKeys)
        {
            if (_player1KeyMap.TryGetValue(key, out var button))
            {
                _controller.SetButtonState(button, true);
            }
            else if (_player2KeyMap.TryGetValue(key, out button))
            {
                _controller.SetButtonState(button, true);
            }
        }
    }

    public void Dispose()
    {
        _isRunning = false;
        _inputThread?.Join(1000); // Wait up to 1 second for thread to finish
    }
}
