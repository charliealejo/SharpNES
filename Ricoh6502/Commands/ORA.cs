namespace Ricoh6502.Commands
{
    public abstract class ORA : BoundaryCheckCommand
    {
        public ORA(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2) { }

        protected override void ExecuteInternal(Processor processor)
        {
            byte value = processor.GetValue(AddressingMode, D1, D2);
            processor.Acc = (byte)(processor.Acc | value);
            processor.Status.SetZeroAndNegativeFlags(processor.Acc);
        }
    }
}