using System.Diagnostics;
using System.Threading;
using Cartridge;
using Logger;
using NESPPU;
using Ricoh6502;

namespace Emulator
{
    public class SharpNesEmu
    {
        private bool _debugMode;
        private ulong _frameCount;
        private readonly CPU _cpu;
        private readonly PPU _ppu;
        private readonly ushort _startAddress;
        private readonly double _clockCycleTimeInNanoSeconds;

        public SharpNesEmu(
            string romPath,
            bool debug = false,
            ushort startAddress = 0)
        {
            _debugMode = debug;

            _cpu = new CPU(debug ? new NesLogger() : null);
            _ppu = new PPU();
            Loader.LoadCartridge(romPath, _cpu.Memory);
            _startAddress = startAddress;

            _clockCycleTimeInNanoSeconds = 1e9 / 1_789_773 / 3;
            _frameCount = 0;
        }

        public void Run()
        {
            _cpu.Reset();
            if (_startAddress > 0)
            {
                _cpu.PC = _startAddress;
            }

            var clock = new Stopwatch();
            var spinWait = new SpinWait();
            var executing = true;
            while (executing)
            {
                clock.Restart();

                _ppu.Clock();
                if (_frameCount % 3 == 0)
                {
                    executing = _cpu.Clock();
                }

                if (!_debugMode)
                {
                    // Wait if needed
                    do
                    {
                        spinWait.SpinOnce();
                    } while (clock.Elapsed.TotalNanoseconds < _clockCycleTimeInNanoSeconds);
                }
            }
        }
    }
}
