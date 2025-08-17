namespace Ricoh6502.Commands
{
    public class LDY : BoundaryCheckCommandBase
    {
        public LDY(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2)
        {
        }

        protected override void ExecuteInternal(Processor processor)
        {
            byte value = processor.GetValue(AddressingMode, D1, D2);
            processor.Y = value;
            processor.Status.SetZeroAndNegativeFlags(value);
        }
    }
}