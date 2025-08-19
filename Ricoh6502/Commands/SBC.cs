namespace Ricoh6502.Commands
{
    public class SBC : BoundaryCheckCommand
    {
        public SBC(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2) { }

        protected override void ExecuteInternal(Processor processor)
        {
            byte prevAcc = processor.Acc;
            byte value = (byte)(~processor.GetValue(AddressingMode, D1, D2) & 0xFF);
            byte carry = processor.Status.CarryFlag ? (byte)1 : (byte)0;
            var result = processor.Acc + value + carry;
            processor.Acc = (byte)(result & 0xFF);
            processor.Status.CarryFlag = !(result < 0);
            processor.Status.OverflowFlag = (((byte)result ^ prevAcc) & ((byte)result ^ ~value) & 0x80) != 0;
            processor.Status.SetZeroAndNegativeFlags(processor.Acc);
        }
    }
}