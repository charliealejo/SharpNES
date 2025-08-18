
namespace Ricoh6502.Commands
{
    public class BRK : Command
    {
        public BRK() : base(AddressingMode.Implied)
        {
        }

        protected override void ExecuteInternal(Processor processor)
        {
            var returnAddress = (ushort)(processor.PC + 2);
            processor.PushStack((byte)(returnAddress >> 8));
            processor.PushStack((byte)returnAddress);
            processor.PushStack(processor.Status.GetStatus());
            processor.Status.InterruptDisable = true;
            processor.Status.BreakCommand = true;
            processor.PC = 0xFFFE;
        }

        protected override byte GetInstructionCycleCount()
        {
            return 7;
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