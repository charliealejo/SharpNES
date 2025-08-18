namespace Ricoh6502.Commands
{
    public class PLA : PullCommand
    {
        public PLA() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Acc = processor.PopStack();
        }
    }
}