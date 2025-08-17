namespace Ricoh6502.Commands
{
    public abstract class ImpliedCommandBase : Command
    {
        public ImpliedCommandBase() : base(AddressingMode.Implied) { }

        protected override byte GetInstructionCycleCount()
        {
            return 2;
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