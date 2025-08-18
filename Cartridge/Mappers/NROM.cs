namespace Cartridge.Mappers
{
    public class NROM : MapperBase
    {
        public NROM(Cartridge cartridge, byte[] cpuMemory) : base(cartridge, cpuMemory)
        {
        }

        public override void MapMemory()
        {
            Array.Copy(_cartridge.PRG_ROM, 0, _cpuMemory, 0x8000, _cartridge.PRG_ROM.Length);
            if (_cartridge.PRG_ROM.Length <= 16384)
            {
                // 16KB PRG ROM
                Array.Copy(_cartridge.PRG_ROM, 0, _cpuMemory, 0xC000, _cartridge.PRG_ROM.Length);
            }
        }
    }
}