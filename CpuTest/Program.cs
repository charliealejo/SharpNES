using Cartridge;
using Ricoh6502;

namespace CpuTest
{
    class Program
    {
        static void Main()
        {
            Processor cpu = new(SystemVersion.NTSC, true);
            Loader.LoadCartridge("testroms/nestest.nes", cpu.Memory);
            cpu.Run(0xC000);
        }
    }
}