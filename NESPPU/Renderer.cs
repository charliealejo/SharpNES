
namespace NESPPU
{
    /// <summary>
    /// Responsible for rendering the graphics for the PPU.
    /// </summary>
    /// <remarks>
    /// PPU CHR-ROM memory map:
    /// <list type="bullet">
    ///     <item>0x0000 - 0x0FFF: Pattern Table 0</item>
    ///     <item>0x1000 - 0x1FFF: Pattern Table 1</item>
    ///     <item>0x2000 - 0x23BF: Nametable 0</item>
    ///     <item>0x23C0 - 0x23FF: Attribute Table 0</item>
    ///     <item>0x2400 - 0x27BF: Nametable 1</item>
    ///     <item>0x27C0 - 0x27FF: Attribute Table 1</item>
    ///     <item>0x2800 - 0x2BBF: Nametable 2</item>
    ///     <item>0x2BC0 - 0x2BFF: Attribute Table 2</item>
    ///     <item>0x2C00 - 0x2FBF: Nametable 3</item>
    ///     <item>0x2FC0 - 0x2FFF: Attribute Table 3</item>
    ///     <item>0x3000 - 0x3EFF: Unused</item>
    ///     <item>0x3F00 - 0x3F1F: Palette RAM indexes</item>
    ///     <item>0x3F20 - 0x3FFF: Mirrors of 0x3F00 - 0x3F1F</item>
    /// </list>
    /// </remarks>
    public class Renderer
    {
        private const int Width = 256;
        private const int Height = 240;

        private readonly uint[] _palette = [
            0x7C7C7C, 0x0000FC, 0x0000BC, 0x4428BC, 0x940084, 0xA80020, 0xA81000, 0x881400,
            0x503000, 0x007800, 0x006800, 0x005800, 0x004058, 0x000000, 0x000000, 0x000000,
            0xBCBCBC, 0x0078F8, 0x0058F8, 0x6844FC, 0xD800CC, 0xE40058, 0xF83800, 0xE45C10,
            0xAC7C00, 0x00B800, 0x00A800, 0x00A844, 0x008888, 0x000000, 0x000000, 0x000000,
            0xF8F8F8, 0x3CBCFC, 0x6888FC, 0x9878F8, 0xF878F8, 0xF85898, 0xF87858, 0xFCA044,
            0xF8B800, 0xB8F818, 0x58D854, 0x58F898, 0x00E8D8, 0x787878, 0x000000, 0x000000,
            0xFCFCFC, 0xA4E4FC, 0xB8B8F8, 0xD8B8F8, 0xF8B8F8, 0xF8A4C0, 0xF0D0B0, 0xFCE0A8,
            0xF8D878, 0xD8F878, 0xB8F8B8, 0xB8F8D8, 0x00FCFC, 0xF8D8F8, 0x000000, 0x000000
        ];

        private PPU _ppu;

        public int[] FrameBuffer { get; private set; }

        public Renderer(PPU ppu)
        {
            _ppu = ppu;
            FrameBuffer = new int[Width * Height];
        }

        public void RenderPixel(int scanline, int dot)
        {
            int x = dot - 1;
            int y = scanline;
            int scrollX = _ppu.Registers.F.HorizontalScroll;
            int scrollY = _ppu.Registers.F.VerticalScroll;

            // Calculate the final pixel position
            int screenX = (x + scrollX) % Width;
            int screenY = (y + scrollY) % Height;

            // Get tile
            int tileX = screenX / 8;
            int tileY = screenY / 8;

            // Calculate offset inside tile
            int tileOffsetX = screenX % 8;
            int tileOffsetY = screenY % 8;

            // Get tile ID from nametable
            ushort nametableAddr = (ushort)(0x2000 + _ppu.Registers.F.BaseNametableAddress);
            ushort tileAddr = (ushort)(nametableAddr + (tileY * 32) + tileX);
            byte tileId = _ppu.Memory[tileAddr];

            // Get pattern data from tile
            ushort patternTableBase = (ushort)(_ppu.Registers.F.BackgroundPatternTableAddress ? 0x1000 : 0x0000);
            ushort patternAddr = (ushort)(patternTableBase + (tileId * 16) + tileOffsetY);

            byte lowByte = _ppu.Memory[patternAddr];      // Bit plane 0
            byte highByte = _ppu.Memory[patternAddr + 8]; // Bit plane 1

            // Extract the specific pixel (bit 7-tileOffsetX because pixels go from left to right)
            int bitIndex = 7 - tileOffsetX;
            int pixel = ((lowByte >> bitIndex) & 1) | (((highByte >> bitIndex) & 1) << 1);

            // Get palette index
            int paletteIndex = GetPaletteIndex(tileX, tileY);
            
            // Calculate the final color
            uint color = GetColor(pixel, paletteIndex);

            // Write to framebuffer
            FrameBuffer[y * 256 + x] = (int)color;
        }

        private int GetPaletteIndex(int tileX, int tileY)
        {
            ushort attributeTableBase = (ushort)(0x23C0 + _ppu.Registers.F.BaseNametableAddress);
            
            // Each byte of the attribute table controls a 4x4 tile area (32x32 pixels)
            int attrX = tileX / 4;
            int attrY = tileY / 4;
            ushort attrAddr = (ushort)(attributeTableBase + (attrY * 8) + attrX);
            byte attrByte = _ppu.Memory[attrAddr];
            
            // Each byte contains 4 palettes of 2 bits each
            int quadrantX = tileX % 4 / 2;
            int quadrantY = tileY % 4 / 2;
            int shift = (quadrantY * 4) + (quadrantX * 2);
            
            return (attrByte >> shift) & 0x03;
        }
        
        private uint GetColor(int pixel, int paletteIndex)
        {
            if (pixel == 0) // Transparent pixel uses universal background color
            {
                return _palette[_ppu.Memory[0x3F00]];
            }

            // Background palettes are in 0x3F01-0x3F0F
            ushort paletteAddr = (ushort)(0x3F01 + (paletteIndex * 4) + (pixel - 1));
            byte colorIndex = _ppu.Memory[paletteAddr];

            return _palette[colorIndex & 0x3F]; // Mask for 64 colors
        }
    }
}