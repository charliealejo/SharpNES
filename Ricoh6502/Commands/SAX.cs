namespace Ricoh6502.Commands
{
    public class SAX : BoundaryCheckCommand
    {
        public SAX(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2) { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.SetValue(AddressingMode, D1, D2, (byte)(processor.Acc & processor.X));
        }
    }
}