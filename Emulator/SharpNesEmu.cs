using System;
using System.Diagnostics;
using System.Threading;
using Cartridge;
using Logger;
using Ricoh6502;

namespace Emulator
{
    public class SharpNesEmu
    {
        private bool _debugMode;
        private readonly Processor _cpu;
        private readonly ushort _startAddress;
        private readonly double _cpuCycleTimeInNanoSeconds;

        public SharpNesEmu(
            string romPath,
            SystemVersion version = SystemVersion.NTSC,
            bool debug = false,
            ushort startAddress = 0)
        {
            _debugMode = debug;

            _cpu = new Processor(version, debug ? new NesLogger() : null);
            Loader.LoadCartridge(romPath, _cpu.Memory);
            _startAddress = startAddress;

            var cpuFrequency = version switch
            {
                SystemVersion.NTSC => 1_789_773,
                SystemVersion.PAL => 1_662_607,
                SystemVersion.Dendy => 1_773_448,
                _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
            };
            _cpuCycleTimeInNanoSeconds = 1e9 / cpuFrequency;
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

                executing = _cpu.Clock();

                if (!_debugMode)
                {
                    // Wait if needed
                    do
                    {
                        spinWait.SpinOnce();
                    } while (clock.Elapsed.TotalNanoseconds < _cpuCycleTimeInNanoSeconds);
                }
            }

        }
    }
}
