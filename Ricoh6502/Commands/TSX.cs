namespace Ricoh6502.Commands
{
    public abstract class TSX : ImpliedCommandBase
    {
        public TSX() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.X = processor.SP;
        }
    }
}