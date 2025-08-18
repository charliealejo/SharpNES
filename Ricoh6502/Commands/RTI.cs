
namespace Ricoh6502.Commands
{
    public class RTI : Command
    {
        public RTI() : base(AddressingMode.Implied)
        {
        }

        protected override void ExecuteInternal(Processor processor)
        {
            var statusByte = processor.PopStack();
            processor.Status.SetStatus(statusByte);
            var returnAddressLow = processor.PopStack();
            var returnAddressHigh = processor.PopStack();
            processor.PC = (ushort)((returnAddressHigh << 8) | returnAddressLow);
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