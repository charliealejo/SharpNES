namespace NESPPU
{
    public class Registers
    {
        private bool _verticalScrollSet = false;
        private bool _ppuAddressFirstWrite = true;

        public byte PPUCTRL // PPU Control Register, CPU address $2000
        {
            set
            {
                F.BaseNametableAddress = (ushort)((value & 0x03) * 0x400);
                F.IncrementMode = (value & 0x04) != 0;
                F.SpritePatternTableAddress = (value & 0x08) != 0;
                F.BackgroundPatternTableAddress = (value & 0x10) != 0;
                F.SpriteSize = (value & 0x20) != 0;
                F.PPUMaster = (value & 0x40) != 0;
                F.NMIEnabled = (value & 0x80) != 0;
            }
        }
        public byte PPUMASK // PPU Mask Register, CPU address $2001
        {
            set
            {
                F.Grayscale = (value & 0x01) != 0;
                F.ShowBackgroundLeft = (value & 0x02) != 0;
                F.ShowSpritesLeft = (value & 0x04) != 0;
                F.ShowBackground = (value & 0x08) != 0;
                F.ShowSprites = (value & 0x10) != 0;
                F.EmphasizeRed = (value & 0x20) != 0;
                F.EmphasizeGreen = (value & 0x40) != 0;
                F.EmphasizeBlue = (value & 0x80) != 0;
            }
        }
        public byte PPUSTATUS { get; } // PPU Status Register, CPU address $2002
        public byte OAMADDR { get; set; } // OAM Address Register, CPU address $2003
        public byte OAMDATA { get; set; } // OAM Data Register, CPU address $2004
        public byte PPUSCROLL // PPU Scroll Register, CPU address $2005
        {
            set
            {
                if (_verticalScrollSet)
                {
                    F.VerticalScroll = value;
                }
                else
                {
                    F.HorizontalScroll = value;
                }
                _verticalScrollSet = !_verticalScrollSet;
            }
        }
        public byte PPUADDR // PPU Address Register, CPU address $2006
        {
            set
            {
                if (_ppuAddressFirstWrite)
                {
                    F.PPUAddress = (ushort)((value & 0x3F) << 8); // Upper 6 bits
                }
                else
                {
                    F.PPUAddress += value; // Lower 8 bits
                }
                _ppuAddressFirstWrite = !_ppuAddressFirstWrite;
            }
        }
        public byte PPUDATA { get; set; } // PPU Data Register, CPU address $2007
        public byte OAMDMA { get; set; } // OAM DMA Register, CPU address $4014

        public Flags F { get; set; } = new Flags();

        public class Flags
        {
            public ushort BaseNametableAddress { get; set; }
            public bool IncrementMode { get; set; } // 0: increment by 1, 1: increment by 32
            public bool SpritePatternTableAddress { get; set; } // 0: $0000, 1: $1000
            public bool BackgroundPatternTableAddress { get; set; } // 0: $0000, 1: $1000
            public bool SpriteSize { get; set; } // 0: 8x8, 1: 8x16
            public bool PPUMaster { get; set; }
            public bool NMIEnabled { get; set; } // NMI on VBlank

            public bool Grayscale { get; set; }
            public bool ShowBackgroundLeft { get; set; }
            public bool ShowSpritesLeft { get; set; }
            public bool ShowBackground { get; set; }
            public bool ShowSprites { get; set; }
            public bool EmphasizeRed { get; set; }
            public bool EmphasizeGreen { get; set; }
            public bool EmphasizeBlue { get; set; }

            public byte HorizontalScroll { get; set; }
            public byte VerticalScroll { get; set; }

            public ushort PPUAddress { get; set; }
        }
    }
}
