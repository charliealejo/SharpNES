namespace Ricoh6502.Commands
{
    public class CLD : ImpliedCommand
    {
        public CLD() : base()
        {
        }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Status.DecimalMode = false;
        }
    }
}
