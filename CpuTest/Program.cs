using Emulator;

namespace CpuTest
{
    class Program
    {
        static void Main()
        {
            var emulator = new SharpNesEmu(
                "testroms/nestest.nes",
                true,
                0xC000);
                
            emulator.Run();
        }
    }
}