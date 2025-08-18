using Cartridge;
using Ricoh6502;

namespace CpuTest
{
    class Program
    {
        static void Main()
        {
            Processor cpu = new(SystemVersion.NTSC);
            Loader.LoadCartridge("testroms/nestest.nes", cpu.Memory);
            cpu.Run();
        }
    }
}