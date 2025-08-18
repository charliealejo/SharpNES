namespace Ricoh6502.Commands
{
    public class CLV : ImpliedCommand
    {
        public CLV() : base()
        {
        }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Status.OverflowFlag = false;
        }
    }
}
