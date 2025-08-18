namespace Ricoh6502.Commands
{
    public class TXS : ImpliedCommand
    {
        public TXS() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.SP = processor.X;
        }
    }
}