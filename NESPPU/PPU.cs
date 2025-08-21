using System;

namespace NESPPU
{
    public class PPU
    {
        private Registers _registers = new Registers();

        private byte[] _OAM = new byte[0x100];

        public byte[] Memory { get; set; } = new byte[0x4000];

        public void Clock()
        {
            // Execute PPU clock cycle
        }
    }
}
