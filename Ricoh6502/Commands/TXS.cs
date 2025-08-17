namespace Ricoh6502.Commands
{
    public abstract class TXS : ImpliedCommandBase
    {
        public TXS() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.SP = processor.X;
        }
    }
}