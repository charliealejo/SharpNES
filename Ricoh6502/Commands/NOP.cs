namespace Ricoh6502.Commands
{
    public class NOP : ImpliedCommand
    {
        public NOP() : base()
        {
        }

        protected override void ExecuteInternal(Processor processor)
        {
            // No operation
        }
    }
}
