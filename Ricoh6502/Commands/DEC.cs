namespace Ricoh6502.Commands
{
    public class DEC : MemoryCommand
    {
        public DEC(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2) { }

        protected override void ExecuteInternal(CPU cpu)
        {
            byte value = cpu.GetValue(AddressingMode, D1, D2);
            cpu.SetValue(AddressingMode, D1, D2, --value);
            cpu.Status.SetZeroAndNegativeFlags(value);
        }
    }
}