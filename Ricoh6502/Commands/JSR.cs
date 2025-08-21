namespace Ricoh6502.Commands
{
    public class JSR : Command
    {
        public JSR(byte d1, byte d2) : base(AddressingMode.Absolute)
        {
            D1 = d1;
            D2 = d2;
        }

        protected override void ExecuteInternal(CPU cpu)
        {
            var returnAddress = (ushort)(cpu.PC + 2);
            cpu.PushStack((byte)(returnAddress >> 8));
            cpu.PushStack((byte)returnAddress);
            cpu.PC = BitConverter.ToUInt16([D1, D2], 0);
        }

        protected override byte GetInstructionCycleCount()
        {
            return 6;
        }

        protected override ushort GetNextInstructionAddress(CPU processor)
        {
            return processor.PC;
        }

        protected override bool CheckForPageBoundaryCrossing(ushort currentInstructionAddress, ushort nextInstructionAddress)
        {
            return false;
        }
    }
}