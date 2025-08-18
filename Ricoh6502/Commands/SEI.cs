namespace Ricoh6502.Commands
{
    public class SEI : ImpliedCommand
    {
        public SEI() : base()
        {
        }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Status.InterruptDisable = true;
        }
    }
}
