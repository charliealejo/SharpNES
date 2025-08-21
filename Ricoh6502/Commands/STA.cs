namespace Ricoh6502.Commands
{
    public class STA : StoreCommand
    {
        public STA(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2) { }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.SetValue(AddressingMode, D1, D2, cpu.Acc);
        }
    }
}