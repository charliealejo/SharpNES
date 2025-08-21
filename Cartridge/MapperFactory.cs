using Cartridge.Mappers;

namespace Cartridge
{
    public static class MapperFactory
    {
        public static MapperBase CreateMapper(Cartridge cartridge, byte[] cpuMemory, byte[] ppuMemory)
        {
            return cartridge.Mapper switch
            {
                0 => new NROM(cartridge, cpuMemory, ppuMemory),
                _ => throw new NotSupportedException($"Unsupported mapper type: {cartridge.Mapper}")
            };
        }
    }
}