namespace Ricoh6502.Commands
{
    public abstract class CMP : BoundaryCheckCommand
    {
        public CMP(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2) { }

        protected override void ExecuteInternal(Processor processor)
        {
            byte value = processor.GetValue(AddressingMode, D1, D2);
            var result = (byte)(processor.Acc - value);
            processor.Status.SetZeroAndNegativeFlags(result);
            processor.Status.CarryFlag = processor.Acc >= value;
        }
    }
}