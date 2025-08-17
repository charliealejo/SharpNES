
namespace Ricoh6502.Commands
{
    public abstract class BranchCommand : Command
    {
        protected bool BranchTaken { get; set; }

        public BranchCommand(byte d1) : base(AddressingMode.Relative)
        {
            D1 = d1;
        }

        protected override void ExecuteInternal(Processor processor)
        {
            if (CheckCondition(processor.Status))
            {
                BranchTaken = true;
                processor.PC = (ushort)(processor.PC + (sbyte)processor.GetValue(AddressingMode, D1, 0));
            }
        }

        protected abstract bool CheckCondition(Status status);

        protected override byte GetInstructionCycleCount()
        {
            return BranchTaken ? (byte)3 : (byte)2;
        }

        protected override ushort GetNextInstructionAddress(Processor processor)
        {
            return (ushort)(processor.PC + 2);
        }

        protected override bool CheckForPageBoundaryCrossing(ushort currentInstructionAddress, ushort nextInstructionAddress)
        {
            return BranchTaken && (nextInstructionAddress & 0xFF00) != (currentInstructionAddress & 0xFF00);
        }
    }
}