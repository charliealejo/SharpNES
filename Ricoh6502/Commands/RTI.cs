
namespace Ricoh6502.Commands
{
    public class RTI : Command
    {
        public RTI() : base(AddressingMode.Implied)
        {
        }

        protected override void ExecuteInternal(CPU cpu)
        {
            var statusByte = cpu.PopStack();
            cpu.Status.SetStatus(statusByte);
            var returnAddressLow = cpu.PopStack();
            var returnAddressHigh = cpu.PopStack();
            cpu.PC = (ushort)((returnAddressHigh << 8) | returnAddressLow);
        }

        protected override byte GetInstructionCycleCount()
        {
            return 6;
        }

        protected override ushort GetNextInstructionAddress(CPU cpu)
        {
            return cpu.PC;
        }

        protected override bool CheckForPageBoundaryCrossing(ushort currentInstructionAddress, ushort nextInstructionAddress)
        {
            return false;
        }
    }
}