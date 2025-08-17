namespace Ricoh6502.Commands
{
    public abstract class CPX : BoundaryCheckCommand
    {
        public CPX(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2) { }

        protected override void ExecuteInternal(Processor processor)
        {
            byte value = processor.GetValue(AddressingMode, D1, D2);
            var result = (byte)(processor.X - value);
            processor.Status.SetZeroAndNegativeFlags(result);
            processor.Status.CarryFlag = processor.X >= value;
        }
    }
}