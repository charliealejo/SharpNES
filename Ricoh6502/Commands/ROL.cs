namespace Ricoh6502.Commands
{
    public abstract class ROL : MemoryCommandBase
    {
        public ROL(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2) { }

        protected override void ExecuteInternal(Processor processor)
        {
            byte value = processor.GetValue(AddressingMode, D1, D2);
            byte result = (byte)(value << 1);
            result |= (byte)(processor.Status.CarryFlag ? 0x01 : 0x00);
            processor.SetValue(AddressingMode, D1, D2, result);
            processor.Status.SetZeroAndNegativeFlags(result);
            processor.Status.CarryFlag = (value & 0x80) != 0;
        }
    }
}