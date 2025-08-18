namespace Ricoh6502.Commands
{
    public class SED : ImpliedCommand
    {
        public SED() : base()
        {
        }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Status.DecimalMode = true;
        }
    }
}
