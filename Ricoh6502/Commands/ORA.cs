namespace Ricoh6502.Commands
{
    public class ORA : BoundaryCheckCommand
    {
        public ORA(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2) { }

        protected override void ExecuteInternal(CPU cpu)
        {
            byte value = cpu.GetValue(AddressingMode, D1, D2);
            cpu.Acc = (byte)(cpu.Acc | value);
            cpu.Status.SetZeroAndNegativeFlags(cpu.Acc);
        }
    }
}