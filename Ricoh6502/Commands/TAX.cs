namespace Ricoh6502.Commands
{
    public class TAX : ImpliedCommand
    {
        public TAX() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.X = processor.Acc;
        }
    }
}