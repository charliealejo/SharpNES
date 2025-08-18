namespace Ricoh6502.Commands
{
    public class CLI : ImpliedCommand
    {
        public CLI() : base()
        {
        }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Status.InterruptDisable = false;
        }
    }
}
