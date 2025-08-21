namespace Cartridge
{
    public static class Loader
    {
        public static void LoadCartridge(string filePath, byte[] cpuMemory, byte[] ppuMemory)
        {
            var cartridge = new Cartridge(filePath);
            var mapper = MapperFactory.CreateMapper(cartridge, cpuMemory, ppuMemory);
            mapper.Initialize();
        }
    }
}