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

            if (ScanLine < 240 && Dot > 0 && Dot <= 256)
            {
                Renderer.RenderPixel(ScanLine, Dot);
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
