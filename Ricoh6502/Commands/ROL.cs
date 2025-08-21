namespace Ricoh6502.Commands
{
    public class ROL : MemoryCommand
    {
        public ROL(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2) { }

        protected override void ExecuteInternal(CPU cpu)
        {
            byte value = cpu.GetValue(AddressingMode, D1, D2);
            byte result = (byte)(value << 1);
            result |= (byte)(cpu.Status.CarryFlag ? 0x01 : 0x00);
            cpu.SetValue(AddressingMode, D1, D2, result);
            cpu.Status.SetZeroAndNegativeFlags(result);
            cpu.Status.CarryFlag = (value & 0x80) != 0;
        }
    }
}