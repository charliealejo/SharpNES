namespace Ricoh6502.Commands
{
    public class JSR : Command
    {
        public JSR(byte d1, byte d2) : base(AddressingMode.Absolute)
        {
            D1 = d1;
            D2 = d2;
        }

        protected override void ExecuteInternal(Processor processor)
        {
            var returnAddress = (ushort)(processor.PC + 2);
            processor.PushStack((byte)(returnAddress >> 8));
            processor.PushStack((byte)returnAddress);
            processor.PC = BitConverter.ToUInt16([D1, D2], 0);
        }

        protected override byte GetInstructionCycleCount()
        {
            return 6;
        }

        protected override ushort GetNextInstructionAddress(Processor processor)
        {
            return processor.PC;
        }

        protected override bool CheckForPageBoundaryCrossing(ushort currentInstructionAddress, ushort nextInstructionAddress)
        {
            return false;
        }
    }
}