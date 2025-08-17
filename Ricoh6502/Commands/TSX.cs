namespace Ricoh6502.Commands
{
    public abstract class TSX : TBase
    {
        public TSX() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.X = processor.SP;
        }
    }
}