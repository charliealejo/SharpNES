namespace NESPPU
{
    public class Registers
    {
        private PPU _ppu;

        public ushort V { get; set; }
        internal ushort T { get; set; }
        internal byte X { get; set; }
        internal bool W { get; set; } = false;

        public byte OpenBus { get; set; }

        public event EventHandler<byte>? PPUStatusChanged;

        public Flags F { get; set; } = new Flags();

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

        private byte _ppuCtrl;
        public byte PPUCTRL // PPU Control Register, CPU address $2000
        {
            get
            {
                return _ppuCtrl;
            }
            set
            {
                _ppuCtrl = value;
                F.BaseNametableAddress = (ushort)((value & 0x03) * 0x400);
                F.IncrementBy32 = (value & 0x04) != 0;
                F.SpritePatternTableAddress = (value & 0x08) != 0;
                F.BackgroundPatternTableAddress = (value & 0x10) != 0;
                F.SpriteSize = (value & 0x20) != 0;
                F.PPUMaster = (value & 0x40) != 0;
                F.NMIEnabled = (value & 0x80) != 0;
                // Bits 10-11 hold the base address of the nametable minus $2000
                T = (ushort)((T & 0xF3FF) | ((value & 0x3) << 10));
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
                byte status = F.CalculatePPUStatus(); // Get bits 7-5 (VBlank, Sprite0Hit, SpriteOverflow)
                byte result = (byte)((status & 0xE0) | (OpenBus & 0x1F)); // Combine with OpenBus bits 0-4
                
                // Update OpenBus with the value we're returning
                OpenBus = result;
                
                F.VBlank = false; // Reading PPUSTATUS clears VBlank flag
                W = false; // Also resets the write toggle
                return result;
            }
            set
            {
                _ppuStatus = value;
                OpenBus = value; // Update OpenBus on write
                PPUStatusChanged?.Invoke(this, value);
            }
        }

        public byte OAMADDR { get; set; } // OAM Address Register, CPU address $2003

        public byte OAMDATA // OAM Data Register, CPU address $2004
        {
            get 
            { 
                byte value = _ppu.OAM[OAMADDR];
                OpenBus = value; // Update OpenBus with read value
                return value;
            }
            set
            {
                _ppu.OAM[OAMADDR] = value;
                OpenBus = value; // Update OpenBus on write
                OAMADDR++;
            }
        }

        public byte PPUSCROLL // PPU Scroll Register, CPU address $2005
        {
            set
            {
                if (W)
                {
                    T = (ushort)((T & 0x8FFF) | ((value & 0x7) << 12));
                    T = (ushort)((T & 0xFC1F) | (value & 0xF8) << 2);
                    F.VerticalScroll = value;
                    W = false;
                }
                else
                {   
                    X = (byte)(value & 0x7);
                    T = (ushort)((T & 0xFFE0) | (value >> 3));
                    F.HorizontalScroll = value;
                    W = true;
                }
            }
        }
        
        public byte PPUADDR // PPU Address Register, CPU address $2006
        {
            set
            {
                if (W)
                {
                    T = (ushort)((T & 0xFF00) | value);
                    V = T;
                    W = false;
                    // Prime the read buffer when address is set
                    _ppuDataBuffer = _ppu.ReadMemory(V);
                }
                else
                {
                    T = (ushort)((T & 0x00FF) | ((value & 0x3F) << 8));
                    W = true;
                }
            }
        }
        
        private byte _ppuDataBuffer = 0;
        public byte PPUDATA // PPU Data Register, CPU address $2007
        {
            get
            {
                byte value;
                if (V < 0x3F00)
                {
                    // Buffered read for everything except palette
                    value = _ppuDataBuffer;
                    _ppuDataBuffer = _ppu.ReadMemory(V);
                }
                else
                {
                    // Immediate read for palette
                    value = _ppu.ReadMemory(V);
                    _ppuDataBuffer = _ppu.ReadMemory((ushort)(V - 0x1000)); // Fill buffer with nametable data
                }
                
                OpenBus = value; // Update OpenBus with read value
                V += (ushort)(F.IncrementBy32 ? 32 : 1);
                return value;
            }
            set
            {
                _ppu.WriteMemory(V, value);
                OpenBus = value; // Update OpenBus on write
                V += (ushort)(F.IncrementBy32 ? 32 : 1);
            }
        }

        public byte OAMDMA { get; set; } // OAM DMA Register, CPU address $4014

        public byte HandleRegisterRead(uint registerIndex)
        {
            byte result = registerIndex switch
            {
                0x2 => PPUSTATUS,    // $2002 - handles OpenBus internally
                0x4 => OAMDATA,      // $2004 - handles OpenBus internally  
                0x7 => PPUDATA,      // $2007 - handles OpenBus internally
                _ => OpenBus         // Other registers return current OpenBus value
            };
            
            // For write-only registers, still return OpenBus but don't modify it
            if (registerIndex == 0x0 || registerIndex == 0x1 || registerIndex == 0x3 || 
                registerIndex == 0x5 || registerIndex == 0x6)
            {
                return OpenBus; // Return current OpenBus without modification
            }
            
            return result;
        }

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

            public ushort HorizontalScroll { get; set; }
            public ushort VerticalScroll { get; set; }
        }
    }
}
