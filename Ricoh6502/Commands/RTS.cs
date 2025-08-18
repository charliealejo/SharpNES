namespace Ricoh6502.Commands
{
    public class RTS : Command
    {
        public RTS() : base(AddressingMode.Implied)
        {
        }

        protected override void ExecuteInternal(Processor processor)
        {
            var returnAddress = (ushort)processor.PopStack();
            returnAddress |= (ushort)(processor.PopStack() << 8);
            processor.PC = returnAddress;
        }

        protected override byte GetInstructionCycleCount()
        {
            return 6;
        }

        protected override ushort GetNextInstructionAddress(Processor processor)
        {
            return (ushort)(processor.PC + 1);
        }

        protected override bool CheckForPageBoundaryCrossing(ushort currentInstructionAddress, ushort nextInstructionAddress)
        {
            return false;
        }
    }
}