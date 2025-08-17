namespace Ricoh6502.Commands
{
    public abstract class TXA : ImpliedCommandBase
    {
        public TXA() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Acc = processor.X;
        }
    }
}