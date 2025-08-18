
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
            processor.PushStack(GetByte(processor.Status));
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

        private static byte GetByte(Status status)
        {
            byte result = 0;
            result |= (byte)(status.CarryFlag ? 1 : 0);
            result |= (byte)(status.ZeroFlag ? 2 : 0);
            result |= (byte)(status.InterruptDisable ? 4 : 0);
            result |= (byte)(status.DecimalMode ? 8 : 0);
            result |= 48;
            result |= (byte)(status.OverflowFlag ? 64 : 0);
            result |= (byte)(status.NegativeFlag ? 128 : 0);
            return result;
        }
    }
}