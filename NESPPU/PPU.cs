namespace NESPPU
{
    public class PPU
    {
        public const int ScanLines = 262;
        public const int Dots = 341;

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

        public Registers Registers { get; private set; }
        public Renderer Renderer { get; private set; }
        public int ScanLine { get; private set; }
        public int Dot { get; private set; }

        public byte[] Memory { get; set; } = new byte[0x4000];
        public byte[] OAM = new byte[0x100];

        public event EventHandler? TriggerNMI;

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

        public void Clock()
        {
            Dot++;

            if (ScanLine < 240)
            {
                Renderer.RenderPixel();
                Renderer.FetchBackgroundData();
            }

            AdvanceDotAndScanLine();
            UpdateRegistersIfNeeded();
        }

        private void AdvanceDotAndScanLine()
        {
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
