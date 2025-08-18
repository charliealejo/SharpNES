namespace Cartridge;

public class Cartridge
{
    public byte[] PRG_ROM { get; set; }
    public byte[] CHR_ROM { get; set; }
    public byte Mapper { get; set; }
    public MirroringType Mirroring { get; set; }

    public Cartridge(string romPath)
    {
        var romData = File.ReadAllBytes(romPath);
        if (romData == null || romData.Length < 16 || !romData[0..4].SequenceEqual(new byte[] { 0x4E, 0x45, 0x53, 0x1A }))
        {
            throw new ArgumentException("Invalid ROM file.", nameof(romPath));
        }

        var prgSize = romData[4] * 16384;
        var chrSize = romData[5] * 8192;
        PRG_ROM = new byte[prgSize];
        CHR_ROM = new byte[chrSize];

        var flags6 = romData[6];
        Mirroring = (flags6 & 1) == 0 ? MirroringType.Horizontal : MirroringType.Vertical;
        if ((flags6 & 8) != 0)
        {
            Mirroring = MirroringType.FourScreen;
        }
        var trainerPresent = (flags6 & 4) != 0;

        var flags7 = romData[7];
        Mapper = (byte)(((flags7 & 0xF0) >> 4) | (flags6 & 0xF0));

        var prgRomStart = 16 + (trainerPresent ? 512 : 0);
        Array.Copy(romData, prgRomStart, PRG_ROM, 0, PRG_ROM.Length);
        Array.Copy(romData, prgRomStart + PRG_ROM.Length, CHR_ROM, 0, CHR_ROM.Length);
    }
}
