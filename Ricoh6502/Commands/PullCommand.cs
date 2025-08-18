namespace Ricoh6502.Commands
{
    public abstract class PullCommand : Command
    {
        public PullCommand() : base(AddressingMode.Implied) { }

        protected override byte GetInstructionCycleCount()
        {
            return 4;
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