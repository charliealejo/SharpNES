namespace NESPPU
{
    public class Registers
    {
        private PPU _ppu;

        internal byte V { get; set; }
        internal byte T { get; set; }
        internal byte X { get; set; }
        internal byte W { get; set; } = 0;

        public byte OpenBus { get; set; }

        public event EventHandler<byte>? PPUStatusChanged;

        public Registers(PPU ppu)
        {
            _ppu = ppu;
            F.PPUStatusChanged += (s, e) =>
            {
                var status = OpenBus & 0x1F; // Preserve lower 5 bits of open bus
                var flags = F.CalculatePPUStatus();
                PPUSTATUS = (byte)(flags | status);
            };
        }

        public byte PPUCTRL // PPU Control Register, CPU address $2000
        {
            set
            {
                F.BaseNametableAddress = (ushort)((value & 0x03) * 0x400);
                F.IncrementBy32 = (value & 0x04) != 0;
                F.SpritePatternTableAddress = (value & 0x08) != 0;
                F.BackgroundPatternTableAddress = (value & 0x10) != 0;
                F.SpriteSize = (value & 0x20) != 0;
                F.PPUMaster = (value & 0x40) != 0;
                F.NMIEnabled = (value & 0x80) != 0;
                W = 0;
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

        private byte _ppuStatus;
        public byte PPUSTATUS
        {
            get
            {
                byte status = _ppuStatus;
                F.VBlank = false; // Reading PPUSTATUS clears VBlank flag
                W = 0; // Also resets the write toggle
                return status;
            }
            set
            {
                _ppuStatus = value;
                PPUStatusChanged?.Invoke(this, value);
            }
        }

        public byte OAMADDR { get; set; } // OAM Address Register, CPU address $2003

        public byte OAMDATA // OAM Data Register, CPU address $2004
        {
            get 
            { 
                return _ppu.OAM[OAMADDR]; 
            }
            set
            {
                _ppu.OAM[OAMADDR] = value;
                OAMADDR++;
            }
        }

        public byte PPUSCROLL // PPU Scroll Register, CPU address $2005
        {
            set
            {
                if (W == 0)
                {
                    F.HorizontalScroll = value;
                    W = 1;
                }
                else
                {
                    F.VerticalScroll = value;
                    W = 0;
                }
            }
        }
        
        public byte PPUADDR // PPU Address Register, CPU address $2006
        {
            set
            {
                if (W == 0)
                {
                    F.PPUAddress = (ushort)((value & 0x3F) << 8); // Upper 6 bits
                    W = 1;
                }
                else
                {
                    F.PPUAddress += value; // Lower 8 bits
                    W = 0;
                }
            }
        }
        
        private byte _ppuDataBuffer = 0;
        public byte PPUDATA // PPU Data Register, CPU address $2007
        {
            get
            {
                byte value;
                if (F.PPUAddress < 0x3F00)
                {
                    // Buffered read for everything except palette
                    value = _ppuDataBuffer;
                    _ppuDataBuffer = _ppu.ReadMemory(F.PPUAddress);
                }
                else
                {
                    // Immediate read for palette
                    value = _ppu.ReadMemory(F.PPUAddress);
                    _ppuDataBuffer = _ppu.ReadMemory((ushort)(F.PPUAddress - 0x1000)); // Fill buffer with nametable data
                }
                F.PPUAddress += (ushort)(F.IncrementBy32 ? 32 : 1);
                return value;
            }
            set
            {
                _ppu.WriteMemory(F.PPUAddress, value);
                F.PPUAddress += (ushort)(F.IncrementBy32 ? 32 : 1);
            }
        }

        public byte OAMDMA { get; set; } // OAM DMA Register, CPU address $4014

        public byte HandleRegisterRead(uint registerIndex)
        {
            return registerIndex switch
            {
                0x2 => PPUSTATUS,    // $2002
                0x4 => OAMDATA,      // $2004  
                0x7 => PPUDATA,      // $2007
                _ => OpenBus         // Other registers return open bus
            };
        }

        public Flags F { get; set; } = new Flags();

        public class Flags
        {
            public event EventHandler? PPUStatusChanged;

            public ushort BaseNametableAddress { get; set; }
            public bool IncrementBy32 { get; set; } // 0: increment by 1, 1: increment by 32
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

            private bool _spriteOverflow;
            public bool SpriteOverflow
            {
                get { return _spriteOverflow; }
                set
                {
                    _spriteOverflow = value;
                    PPUStatusChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            private bool _sprite0Hit;
            public bool Sprite0Hit
            {
                get { return _sprite0Hit; }
                set
                {
                    _sprite0Hit = value;
                    PPUStatusChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            private bool _vBlank;
            public bool VBlank
            {
                get { return _vBlank; }
                set
                {
                    _vBlank = value;
                    PPUStatusChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            public byte CalculatePPUStatus()
            {
                byte status = 0;
                if (SpriteOverflow) status |= 0x20;
                if (Sprite0Hit) status |= 0x40;
                if (VBlank) status |= 0x80;
                return status;
            }

            public byte HorizontalScroll { get; set; }
            public byte VerticalScroll { get; set; }

            public ushort PPUAddress { get; set; }
        }
    }
}
