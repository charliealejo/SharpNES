namespace Ricoh6502.Commands
{
    public class LAX : BoundaryCheckCommand
    {
        public LAX(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2) { }

        protected override void ExecuteInternal(CPU cpu)
        {
            byte value = cpu.GetValue(AddressingMode, D1, D2);
            cpu.Acc = value;
            cpu.X = value;
            cpu.Status.SetZeroAndNegativeFlags(value);
        }
    }
}