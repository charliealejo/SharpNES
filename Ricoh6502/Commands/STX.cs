namespace Ricoh6502.Commands
{
    public class STX : STBase
    {
        public STX(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2)
        {
        }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.SetValue(AddressingMode, D1, D2, processor.X);
        }
    }
}