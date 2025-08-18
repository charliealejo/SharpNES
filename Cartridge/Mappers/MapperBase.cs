namespace Cartridge.Mappers;

public abstract class MapperBase
{
    protected readonly Cartridge _cartridge;
    protected readonly byte[] _cpuMemory;

    protected MapperBase(Cartridge cartridge, byte[] cpuMemory)
    {
        _cartridge = cartridge ?? throw new ArgumentNullException(nameof(cartridge));
        _cpuMemory = cpuMemory ?? throw new ArgumentNullException(nameof(cpuMemory));
    }

    public void Initialize()
    {
        if (_cartridge.PRG_ROM.Length == 0 || _cartridge.CHR_ROM.Length == 0)
        {
            throw new ArgumentException("Invalid cartridge data.", nameof(_cartridge));
        }

        MapMemory();
    }

    public abstract void MapMemory();
}