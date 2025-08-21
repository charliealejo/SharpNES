namespace Ricoh6502.Commands
{
    public class RTS : Command
    {
        public RTS() : base(AddressingMode.Implied)
        {
        }

        protected override void ExecuteInternal(CPU cpu)
        {
            var returnAddress = (ushort)cpu.PopStack();
            returnAddress |= (ushort)(cpu.PopStack() << 8);
            cpu.PC = returnAddress;
        }

        protected override byte GetInstructionCycleCount()
        {
            return 6;
        }

        protected override ushort GetNextInstructionAddress(CPU cpu)
        {
            return (ushort)(cpu.PC + 1);
        }

        protected override bool CheckForPageBoundaryCrossing(ushort currentInstructionAddress, ushort nextInstructionAddress)
        {
            return false;
        }
    }
}