namespace Ricoh6502.Commands
{
    public class BIT : BoundaryCheckCommand
    {
        public BIT(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2) { }

        protected override void ExecuteInternal(CPU cpu)
        {
            byte value = cpu.GetValue(AddressingMode, D1, D2);
            byte result = (byte)(cpu.Acc & value);
            cpu.Status.ZeroFlag = result == 0;
            cpu.Status.NegativeFlag = (value & 0x80) != 0;
            cpu.Status.OverflowFlag = (value & 0x40) != 0;
        }
    }
}