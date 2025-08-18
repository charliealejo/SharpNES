namespace Ricoh6502.Commands
{
    public abstract class PushCommand : Command
    {
        public PushCommand() : base(AddressingMode.Implied) { }

        protected override byte GetInstructionCycleCount()
        {
            return 3;
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