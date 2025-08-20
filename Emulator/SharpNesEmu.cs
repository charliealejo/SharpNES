using Cartridge;
using Logger;
using Ricoh6502;

namespace Emulator
{
    public class SharpNesEmu
    {
        private readonly Processor _cpu;
        private readonly ushort _startAddress;

        public SharpNesEmu(string romPath, bool debug = false, ushort startAddress = 0)
        {
            _cpu = new Processor(SystemVersion.NTSC, debug ? new NesLogger() : null);
            Loader.LoadCartridge(romPath, _cpu.Memory);
            _startAddress = startAddress;
        }

        public void Run()
        {
            _cpu.Run(_startAddress);
        }
    }
}
