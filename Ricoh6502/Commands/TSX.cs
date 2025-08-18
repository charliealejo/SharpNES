namespace Ricoh6502.Commands
{
    public class TSX : ImpliedCommand
    {
        public TSX() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.X = processor.SP;
        }
    }
}