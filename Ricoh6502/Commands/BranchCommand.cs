
namespace Ricoh6502.Commands
{
    public abstract class BranchCommand : Command
    {
        protected bool BranchTaken { get; set; }

        public BranchCommand(byte d1) : base(AddressingMode.Relative)
        {
            D1 = d1;
        }

        protected override void ExecuteInternal(CPU cpu)
        {
            if (CheckCondition(cpu.Status))
            {
                BranchTaken = true;
                cpu.PC = (ushort)(cpu.PC + (sbyte)D1);
            }
        }

        protected abstract bool CheckCondition(Status status);

        protected override byte GetInstructionCycleCount()
        {
            return BranchTaken ? (byte)3 : (byte)2;
        }

        protected override ushort GetNextInstructionAddress(CPU cpu)
        {
            return (ushort)(cpu.PC + 2);
        }

        protected override bool CheckForPageBoundaryCrossing(ushort currentInstructionAddress, ushort nextInstructionAddress)
        {
            return BranchTaken && (nextInstructionAddress & 0xFF00) != (currentInstructionAddress & 0xFF00);
        }
    }
}