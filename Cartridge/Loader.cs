namespace Cartridge
{
    public static class Loader
    {
        public static void LoadCartridge(string filePath, byte[] cpuMemory)
        {
            var cartridge = new Cartridge(filePath);
            var mapper = MapperFactory.CreateMapper(cartridge, cpuMemory);
            mapper.Initialize();
        }
    }
}