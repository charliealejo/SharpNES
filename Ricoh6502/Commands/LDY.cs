namespace Ricoh6502.Commands
{
    public class LDY : BoundaryCheckCommand
    {
        public LDY(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2)
        {
        }

        protected override void ExecuteInternal(CPU cpu)
        {
            byte value = cpu.GetValue(AddressingMode, D1, D2);
            cpu.Y = value;
            cpu.Status.SetZeroAndNegativeFlags(value);
        }
    }
}