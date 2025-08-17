namespace Ricoh6502.Commands
{
    public class LDA : BoundaryCheckCommand
    {
        public LDA(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2) { }

        protected override void ExecuteInternal(Processor processor)
        {
            byte value = processor.GetValue(AddressingMode, D1, D2);
            processor.Acc = value;
            processor.Status.SetZeroAndNegativeFlags(value);
        }
    }
}