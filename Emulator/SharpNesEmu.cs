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
        private readonly ushort _startAddress;
        private readonly double _clockCycleTimeInNanoSeconds;

        public CPU CPU { get; set; }
        public PPU PPU { get; set; }

        public SharpNesEmu(
            string romPath,
            bool debug = false,
            ushort startAddress = 0)
        {
            _debugMode = debug;

            CPU = new CPU(debug ? new NesLogger() : null);
            PPU = new PPU();
            Loader.LoadCartridge(romPath, CPU.Memory, PPU.Memory);
            _startAddress = startAddress;

            var memoryBus = new MemoryBus(CPU, PPU);
            memoryBus.Initialize();

            _clockCycleTimeInNanoSeconds = 1e9 / 1_789_773 / 3;
            _frameCount = 0;
        }

        public void Run()
        {
            CPU.Reset();
            if (_startAddress > 0)
            {
                CPU.PC = _startAddress;
            }

            var clock = new Stopwatch();
            var spinWait = new SpinWait();
            var executing = true;
            while (executing)
            {
                clock.Restart();

                PPU.Clock();
                if (_frameCount % 3 == 0)
                {
                    executing = CPU.Clock();
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
