namespace Cartridge;

public class Cartridge
{
    public byte[] PRG_ROM { get; set; }
    public byte[] CHR_ROM { get; set; }
    public int Mapper { get; set; }
    public MirroringType Mirroring { get; set; }
    public bool HasBattery { get; set; }
    public bool HasTrainer { get; set; }
    public INESVersion Version { get; set; }
    public int Submapper { get; set; } // iNES 2.0 only
    public int PRGRamSize { get; set; } // iNES 2.0 only
    public int CHRRamSize { get; set; } // iNES 2.0 only

    public Cartridge(string romPath)
    {
        var romData = File.ReadAllBytes(romPath);
        if (romData == null || romData.Length < 16 || !romData[0..4].SequenceEqual(new byte[] { 0x4E, 0x45, 0x53, 0x1A }))
        {
            throw new ArgumentException("Invalid ROM file.", nameof(romPath));
        }

        var flags6 = romData[6];
        var flags7 = romData[7];
        var flags8 = romData[8];
        var flags9 = romData[9];
        var flags10 = romData[10];

        // Determine iNES version
        if ((flags7 & 0x0C) == 0x08)
        {
            Version = INESVersion.iNES2_0;
        }
        else
        {
            Version = INESVersion.iNES1_0;
        }

        // Parse common fields
        HasTrainer = (flags6 & 0x04) != 0;
        HasBattery = (flags6 & 0x02) != 0;
        
        // Mirroring
        Mirroring = (flags6 & 1) == 0 ? MirroringType.Horizontal : MirroringType.Vertical;
        if ((flags6 & 8) != 0)
        {
            Mirroring = MirroringType.FourScreen;
        }

        int prgSize, chrSize;

        if (Version == INESVersion.iNES2_0)
        {
            // iNES 2.0 format
            var prgSizeLow = romData[4];
            var chrSizeLow = romData[5];
            var prgSizeHigh = (flags9 & 0x0F) << 8;
            var chrSizeHigh = (flags9 & 0xF0) << 4;

            prgSize = (prgSizeHigh | prgSizeLow) * 16384;
            chrSize = (chrSizeHigh | chrSizeLow) * 8192;

            // Mapper number (12 bits in iNES 2.0)
            var mapperLow = (flags6 & 0xF0) >> 4;
            var mapperMid = flags7 & 0xF0;
            var mapperHigh = (flags8 & 0x0F) << 8;
            Mapper = mapperHigh | mapperMid | mapperLow;

            // Submapper (4 bits)
            Submapper = (flags8 & 0xF0) >> 4;

            // PRG-RAM size (iNES 2.0)
            var prgRamShift = flags10 & 0x0F;
            PRGRamSize = prgRamShift == 0 ? 0 : 64 << prgRamShift;

            // CHR-RAM size (iNES 2.0)
            var chrRamShift = (flags10 & 0xF0) >> 4;
            CHRRamSize = chrRamShift == 0 ? 0 : 64 << chrRamShift;
        }
        else
        {
            // iNES 1.0 format
            prgSize = romData[4] * 16384;
            chrSize = romData[5] * 8192;

            // Mapper number (8 bits in iNES 1.0)
            Mapper = ((flags7 & 0xF0) >> 4) | (flags6 & 0xF0);
            Submapper = 0; // Not supported in iNES 1.0

            // PRG-RAM size (iNES 1.0 - basic support)
            PRGRamSize = flags8 == 0 ? 8192 : flags8 * 8192; // Default to 8KB if 0
            CHRRamSize = chrSize == 0 ? 8192 : 0; // CHR-RAM if no CHR-ROM
        }

        // Allocate ROM arrays
        PRG_ROM = new byte[prgSize];
        CHR_ROM = new byte[chrSize];

        // Copy ROM data
        var romStart = 16 + (HasTrainer ? 512 : 0);
        Array.Copy(romData, romStart, PRG_ROM, 0, PRG_ROM.Length);
        if (chrSize > 0)
        {
            Array.Copy(romData, romStart + PRG_ROM.Length, CHR_ROM, 0, CHR_ROM.Length);
        }
    }
}

public enum INESVersion
{
    iNES1_0,
    iNES2_0
}
