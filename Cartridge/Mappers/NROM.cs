namespace Cartridge.Mappers
{
    public class NROM : MapperBase
    {
        public NROM(Cartridge cartridge, byte[] cpuMemory, byte[] ppuMemory) : base(cartridge, cpuMemory, ppuMemory)
        {
        }

        public override void MapMemory()
        {
            Array.Copy(_cartridge.PRG_ROM, 0, _cpuMemory, 0x8000, _cartridge.PRG_ROM.Length);
            if (_cartridge.PRG_ROM.Length <= 16384)
            {
                // 16KB PRG ROM - mirror it to $C000
                Array.Copy(_cartridge.PRG_ROM, 0, _cpuMemory, 0xC000, _cartridge.PRG_ROM.Length);
            }

            // Handle CHR data
            if (_cartridge.CHR_ROM != null && _cartridge.CHR_ROM.Length > 0)
            {
                Array.Copy(_cartridge.CHR_ROM, 0, _ppuMemory, 0x0000, Math.Min(_cartridge.CHR_ROM.Length, 0x2000));
            }
            // If no CHR-ROM, CHR-RAM should be initialized (already zeroed by default)
        }
    }
}