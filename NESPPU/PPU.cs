using Cartridge;

namespace NESPPU
{
    public class PPU
    {
        public const int ScanLines = 262;
        public const int Dots = 341;

        public Registers Registers { get; private set; }
        public Renderer Renderer { get; private set; }
        public int ScanLine { get; private set; }
        public int Dot { get; private set; }

        private byte[] _memory = new byte[0x4000];
        public byte[] Memory { get { return _memory; } }
        public byte[] OAM = new byte[0x100];
        
        // Mirroring configuration - set by cartridge loader
        public MirroringType Mirroring { get; set; }

        public event EventHandler? TriggerNMI;
        public event EventHandler<int[]>? FrameCompleted;

        public PPU()
        {
            Registers = new Registers(this);
            Renderer = new Renderer(this);
        }

        public void Reset()
        {
            ScanLine = 0;
            Dot = 0;
        }

        public byte ReadMemory(ushort address)
        {
            address = MirrorAddress(address);
            return _memory[address];
        }

        public void WriteMemory(ushort address, byte value)
        {
            address = MirrorAddress(address);
            _memory[address] = value;
        }

        private ushort MirrorAddress(ushort address)
        {
            // Handle PPU memory mirroring
            if (address >= 0x3000 && address <= 0x3EFF)
            {
                // $3000-$3EFF mirrors $2000-$2EFF
                address = (ushort)(0x2000 + (address - 0x3000));
            }
            else if (address >= 0x3F20 && address <= 0x3FFF)
            {
                // $3F20-$3FFF mirrors $3F00-$3F1F (palette RAM)
                address = (ushort)(0x3F00 + ((address - 0x3F00) % 0x20));
            }

            // Handle nametable mirroring ($2000-$2FFF)
            if (address >= 0x2000 && address <= 0x2FFF)
            {
                address = MirrorNametable(address);
            }

            return address;
        }

        private ushort MirrorNametable(ushort address)
        {
            var nametableIndex = (address - 0x2000) / 0x400; // Which nametable (0-3)
            var offset = (address - 0x2000) % 0x400;         // Offset within nametable

            switch (Mirroring)
            {
                case MirroringType.Horizontal:
                    // NT0&1 share memory, NT2&3 share memory
                    if (nametableIndex == 1) nametableIndex = 0; // NT1 -> NT0
                    if (nametableIndex == 3) nametableIndex = 2; // NT3 -> NT2
                    break;

                case MirroringType.Vertical:
                    // NT0&2 share memory, NT1&3 share memory
                    if (nametableIndex == 2) nametableIndex = 0; // NT2 -> NT0
                    if (nametableIndex == 3) nametableIndex = 1; // NT3 -> NT1
                    break;

                case MirroringType.SingleScreen:
                    // All nametables point to NT0
                    nametableIndex = 0;
                    break;

                case MirroringType.FourScreen:
                    // No mirroring - each nametable has its own memory
                    break;
            }

            return (ushort)(0x2000 + nametableIndex * 0x400 + offset);
        }

        public void Clock()
        {
            if (ScanLine < 240 && Dot == 0)
            {
                Renderer.EvaluateSprites(ScanLine);
            }

            if (ScanLine < 240 && Dot > 0 && Dot <= 256)
            {
                Renderer.RenderPixel(ScanLine, Dot);
            }
            else if (ScanLine == 240 && Dot == 1)
            {
                FrameCompleted?.Invoke(this, Renderer.FrameBuffer);
            }

            AdvanceDotAndScanLine();
            UpdateRegistersIfNeeded();
        }

        private void AdvanceDotAndScanLine()
        {
            Dot++;

            if (Dot >= Dots)
            {
                Dot = 0;
                ScanLine++;
            }

            if (ScanLine >= ScanLines)
            {
                ScanLine = 0;
            }
        }

        private void UpdateRegistersIfNeeded()
        {
            if (ScanLine == 241 && Dot == 1)
            {
                if (Registers.F.NMIEnabled)
                {
                    TriggerNMI?.Invoke(this, EventArgs.Empty);
                }
                Registers.F.VBlank = true;
            }
            if (ScanLine == 261 && Dot == 1)
            {
                Registers.F.VBlank = false;
                Registers.F.Sprite0Hit = false;
                Registers.F.SpriteOverflow = false;
            }
            if ((ScanLine == 261 || ScanLine < 240) && Dot >= 257 && Dot <= 320)
            {
                Registers.OAMADDR = 0;
            }
        }
    }
}
