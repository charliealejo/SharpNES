namespace Ricoh6502.Commands
{
    public class TXA : ImpliedCommand
    {
        public TXA() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Acc = processor.X;
            processor.Status.SetZeroAndNegativeFlags(processor.Acc);
        }
    }
}