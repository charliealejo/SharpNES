namespace Ricoh6502.Commands
{
    public class PLP : PullCommand
    {
        public PLP() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Status.SetStatus(processor.PopStack());
        }
    }
}