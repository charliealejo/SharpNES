
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
            RecoverStatus(processor, statusByte);
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

        private static void RecoverStatus(Processor processor, byte statusByte)
        {
            processor.Status.CarryFlag = (statusByte & 1) != 0;
            processor.Status.ZeroFlag = (statusByte & 2) != 0;
            processor.Status.InterruptDisable = (statusByte & 4) != 0;
            processor.Status.DecimalMode = (statusByte & 8) != 0;
            processor.Status.BreakCommand = false;
            processor.Status.OverflowFlag = (statusByte & 64) != 0;
            processor.Status.NegativeFlag = (statusByte & 128) != 0;
        }
    }
}