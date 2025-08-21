namespace Ricoh6502.Commands
{
    public class CMP : BoundaryCheckCommand
    {
        public CMP(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2) { }

        protected override void ExecuteInternal(CPU cpu)
        {
            byte value = cpu.GetValue(AddressingMode, D1, D2);
            var result = (byte)(cpu.Acc - value);
            cpu.Status.SetZeroAndNegativeFlags(result);
            cpu.Status.CarryFlag = cpu.Acc >= value;
        }
    }
}