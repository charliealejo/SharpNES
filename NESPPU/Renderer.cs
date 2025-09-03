using Cartridge;

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

        // Memory address constants
        private const ushort PatternTable0Base = 0x0000;
        private const ushort PatternTable1Base = 0x1000;
        private const ushort Nametable0Base = 0x2000;
        private const ushort AttributeTable0Base = 0x23C0;
        private const ushort UniversalBackgroundColorAddr = 0x3F00;
        private const ushort BackgroundPaletteBase = 0x3F01;
        private const ushort SpritePaletteBase = 0x3F11;
        
        // Sprite attribute bit masks
        private const byte SpritePaletteMask = 0x03;
        private const byte SpritePriorityMask = 0x20;
        private const byte SpriteHorizontalFlipMask = 0x40;
        private const byte SpriteVerticalFlipMask = 0x80;

        // Structure for evaluated sprites
        private struct Sprite
        {
            public byte X;
            public byte Y;
            public byte TileIndex;
            public byte Attributes;
            public bool IsSprite0;
        }

        private readonly Sprite[] _activeSprites = new Sprite[8];
        private int _activeSpriteCount = 0;

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

            // Render background
            uint backgroundColor = RenderBackgroundPixel(x, y);

            // Render sprites
            uint spriteColor = RenderSpritePixel(x, y, out bool spritePriority, out bool isSprite0);

            // Combine background and sprite
            uint finalColor;
            if (spriteColor != 0) // Sprite is not transparent
            {
                if (spritePriority || backgroundColor == 0) // Sprite has priority or background is transparent
                {
                    finalColor = spriteColor;
                }
                else
                {
                    finalColor = backgroundColor;
                }
                
                // Detect sprite 0 hit
                if (isSprite0 && backgroundColor != 0 && x < 255)
                {
                    _ppu.Registers.F.Sprite0Hit = true;
                }
            }
            else
            {
                finalColor = backgroundColor;
            }

            FrameBuffer[y * 256 + x] = (int)finalColor;
        }

        public void EvaluateSprites(int scanline)
        {
            _activeSpriteCount = 0;
            
            // Evaluate all sprites in OAM
            for (int i = 0; i < 64 && _activeSpriteCount < 8; i++)
            {
                int oamIndex = i * 4;
                byte spriteY = _ppu.OAM[oamIndex];
                byte tileIndex = _ppu.OAM[oamIndex + 1];
                byte attributes = _ppu.OAM[oamIndex + 2];
                byte spriteX = _ppu.OAM[oamIndex + 3];
                
                int spriteHeight = _ppu.Registers.F.SpriteSize ? 16 : 8;
                
                // Check if the sprite is on this scanline
                // NES sprites appear on scanline Y+1, not Y
                if (scanline >= spriteY + 1 && scanline < spriteY + 1 + spriteHeight)
                {
                    _activeSprites[_activeSpriteCount] = new Sprite
                    {
                        Y = spriteY,
                        TileIndex = tileIndex,
                        Attributes = attributes,
                        X = spriteX,
                        IsSprite0 = (i == 0) // The first sprite is sprite 0
                    };
                    _activeSpriteCount++;
                }
            }
            
            // Sprite overflow flag
            if (_activeSpriteCount >= 8)
            {
                _ppu.Registers.F.SpriteOverflow = true;
            }
        }

        private uint RenderBackgroundPixel(int x, int y)
        {
            if (!_ppu.Registers.F.ShowBackground) return 0;
            if (!_ppu.Registers.F.ShowBackgroundLeft && x < 8) return 0;
            
            // Calculate total scroll in pixels from coarse/fine components
            int coarseX = (_ppu.Registers.F.HorizontalScroll >> 3) & 0x1F; // Coarse X (0-31)
            int fineX = _ppu.Registers.F.HorizontalScroll & 0x07;         // Fine X (0-7)
            int totalScrollX = coarseX * 8 + fineX;
            
            int coarseY = (_ppu.Registers.F.VerticalScroll >> 3) & 0x1F; // Coarse Y (0-29)
            int fineY = _ppu.Registers.F.VerticalScroll & 0x07;         // Fine Y (0-7)
            int totalScrollY = coarseY * 8 + fineY;

            // Compute effective positions including total scroll
            int effectiveX = x + totalScrollX;
            int effectiveY = y + totalScrollY;

            // Determine nametable offsets based on effective positions
            int nametableX = effectiveX / 256;
            int nametableY = effectiveY / 240;

            // Wrap screen coordinates within a single nametable
            int screenX = effectiveX % 256;
            int screenY = effectiveY % 240;

            // Calculate nametable index (0-3), starting from base and adjusting for scroll
            int baseNametableIndex = _ppu.Registers.F.BaseNametableAddress / 0x400; // 0-3
            int scrolledNametableIndex = baseNametableIndex + ((nametableY % 2) << 1) + (nametableX % 2);

            // Apply mirroring based on cartridge type (from PPU.Mirroring, set by Cartridge)
            switch (_ppu.Mirroring)
            {
                case MirroringType.Horizontal:
                    // Mirrors nametables 0<->1, 2<->3: Force even indices
                    scrolledNametableIndex = (scrolledNametableIndex / 2) * 2;
                    break;
                case MirroringType.Vertical:
                    // Mirrors nametables 0<->2, 1<->3: Force indices 0 or 1
                    scrolledNametableIndex %= 2;
                    break;
                case MirroringType.FourScreen:
                default:
                    // No mirroring: Wrap 0-3 normally
                    scrolledNametableIndex %= 4;
                    break;
            }

            // Compute addresses for the correct nametable
            ushort nametableAddr = (ushort)(Nametable0Base + scrolledNametableIndex * 0x400);
            ushort attributeTableAddr = (ushort)(AttributeTable0Base + scrolledNametableIndex * 0x400);

            // Rest of the method uses screenX/screenY instead of the old wrapped values
            int tileX = screenX / 8;
            int tileY = screenY / 8;
            int tileOffsetX = screenX % 8;
            int tileOffsetY = screenY % 8;

            ushort tileAddr = (ushort)(nametableAddr + (tileY * 32) + tileX);
            byte tileId = _ppu.ReadMemory(tileAddr);

            ushort patternTableBase = _ppu.Registers.F.BackgroundPatternTableAddress ? PatternTable1Base : PatternTable0Base;
            ushort patternAddr = (ushort)(patternTableBase + (tileId * 16) + tileOffsetY);

            byte lowByte = _ppu.ReadMemory(patternAddr);
            byte highByte = _ppu.ReadMemory((ushort)(patternAddr + 8));

            int bitIndex = 7 - tileOffsetX;
            int pixel = ((lowByte >> bitIndex) & 1) | (((highByte >> bitIndex) & 1) << 1);

            int paletteIndex = GetPaletteIndex(tileX, tileY, attributeTableAddr);
            return GetBackgroundColor(pixel, paletteIndex);
        }

        private uint RenderSpritePixel(int x, int y, out bool priority, out bool isSprite0)
        {
            priority = false;
            isSprite0 = false;

            if (!_ppu.Registers.F.ShowSprites) return 0;
            if (!_ppu.Registers.F.ShowSpritesLeft && x < 8) return 0;

            // Search for sprites at this position (from highest to lowest priority)
            for (int i = _activeSpriteCount - 1; i >= 0; i--)
            {
                var sprite = _activeSprites[i];
                
                // Check if the pixel is inside the sprite
                int spriteHeight = _ppu.Registers.F.SpriteSize ? 16 : 8;
                if (x >= sprite.X && x < sprite.X + 8 && 
                    y >= sprite.Y + 1 && y < sprite.Y + 1 + spriteHeight) // Adjust Y position
                {
                    int spriteX = x - sprite.X;
                    int spriteY = y - (sprite.Y + 1); // Adjust Y calculation
                    
                    // Horizontal flip
                    if ((sprite.Attributes & SpriteHorizontalFlipMask) != 0)
                    {
                        spriteX = 7 - spriteX;
                    }
                    
                    // Vertical flip
                    if ((sprite.Attributes & SpriteVerticalFlipMask) != 0)
                    {
                        spriteY = spriteHeight - 1 - spriteY;
                    }
                    
                    uint color = GetSpritePixelColor(sprite, spriteX, spriteY);
                    if (color != 0) // Not transparent
                    {
                        priority = (sprite.Attributes & SpritePriorityMask) == 0; // Bit 5 = 0 means priority over background
                        isSprite0 = sprite.IsSprite0;
                        return color;
                    }
                }
            }
            
            return 0; // Transparent
        }

        private uint GetSpritePixelColor(Sprite sprite, int spriteX, int spriteY)
        {
            ushort patternTableBase = _ppu.Registers.F.SpritePatternTableAddress ? PatternTable1Base : PatternTable0Base;
            
            byte tileIndex = sprite.TileIndex;
            
            // For 8x16 sprites, handle double tiles
            if (_ppu.Registers.F.SpriteSize)
            {
                patternTableBase = (tileIndex & 1) == 1 ? PatternTable1Base : PatternTable0Base;
                tileIndex &= 0xFE; // Even tile for the top part
                
                if (spriteY >= 8)
                {
                    tileIndex++; // Next tile for the bottom part
                    spriteY -= 8;
                }
            }
            
            ushort patternAddr = (ushort)(patternTableBase + (tileIndex * 16) + spriteY);

            byte lowByte = _ppu.ReadMemory(patternAddr);
            byte highByte = _ppu.ReadMemory((ushort)(patternAddr + 8));

            int bitIndex = 7 - spriteX;
            int pixel = ((lowByte >> bitIndex) & 1) | (((highByte >> bitIndex) & 1) << 1);
            
            if (pixel == 0) return 0; // Transparent
            
            // Sprite palettes are at 0x3F11-0x3F1F
            int paletteIndex = sprite.Attributes & SpritePaletteMask;
            ushort paletteAddr = (ushort)(SpritePaletteBase + (paletteIndex * 4) + (pixel - 1));
            byte colorIndex = _ppu.ReadMemory(paletteAddr);

            return _palette[colorIndex & 0x3F];
        }

        private int GetPaletteIndex(int tileX, int tileY, ushort attributeTableAddr)
        {
            // Each byte of the attribute table controls a 4x4 tile area (32x32 pixels)
            int attrX = tileX / 4;
            int attrY = tileY / 4;
            ushort attrAddr = (ushort)(attributeTableAddr + (attrY * 8) + attrX);
            byte attrByte = _ppu.ReadMemory(attrAddr);

            // Each byte contains 4 palettes of 2 bits each
            int quadrantX = tileX % 4 / 2;
            int quadrantY = tileY % 4 / 2;
            int shift = (quadrantY * 4) + (quadrantX * 2);
            
            return (attrByte >> shift) & 0x03;
        }

        private uint GetBackgroundColor(int pixel, int paletteIndex)
        {
            if (pixel == 0)
            {
                return _palette[_ppu.ReadMemory(UniversalBackgroundColorAddr)];
            }

            ushort paletteAddr = (ushort)(BackgroundPaletteBase + (paletteIndex * 4) + (pixel - 1));
            byte colorIndex = _ppu.ReadMemory(paletteAddr);
            return _palette[colorIndex & 0x3F];
        }
    }
}