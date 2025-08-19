namespace Ricoh6502.Commands
{
    public class BIT : BoundaryCheckCommand
    {
        public BIT(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2) { }

        protected override void ExecuteInternal(Processor processor)
        {
            byte value = processor.GetValue(AddressingMode, D1, D2);
            byte result = (byte)(processor.Acc & value);
            processor.Status.ZeroFlag = result == 0;
            processor.Status.NegativeFlag = (result & 0x80) != 0;
            processor.Status.OverflowFlag = (value & 0x40) != 0;
        }
    }
}