using Cartridge;
using Logger;
using Ricoh6502;

namespace CpuTest
{
    class Program
    {
        static void Main()
        {
            Processor cpu = new(SystemVersion.NTSC, new NesLogger());
            Loader.LoadCartridge("testroms/nestest.nes", cpu.Memory);
            cpu.Run(0xC000);
        }
    }
}