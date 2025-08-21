namespace Ricoh6502.Commands
{
    public class CPX : BoundaryCheckCommand
    {
        public CPX(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2) { }

        protected override void ExecuteInternal(CPU cpu)
        {
            byte value = cpu.GetValue(AddressingMode, D1, D2);
            var result = (byte)(cpu.X - value);
            cpu.Status.SetZeroAndNegativeFlags(result);
            cpu.Status.CarryFlag = cpu.X >= value;
        }
    }
}