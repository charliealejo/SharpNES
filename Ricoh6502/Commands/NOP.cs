namespace Ricoh6502.Commands
{
    public class NOP : BoundaryCheckCommand
    {
        public NOP(
            AddressingMode addressingMode = AddressingMode.Implied,
            byte d1 = 0,
            byte d2 = 0) : base(addressingMode, d1, d2)
        {
        }

        protected override void ExecuteInternal(CPU cpu)
        {
            // No operation
        }
    }
}
