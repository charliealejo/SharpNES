namespace Ricoh6502.Commands
{
    public class JMP : Command
    {
        public JMP(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode)
        {
            D1 = d1;
            D2 = d2;
        }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.PC = processor.GetValue(AddressingMode, D1, D2);
        }

        protected override byte GetInstructionCycleCount()
        {
            return AddressingMode == AddressingMode.Absolute ? (byte)3 : (byte)5;
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