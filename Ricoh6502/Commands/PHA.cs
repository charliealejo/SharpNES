namespace Ricoh6502.Commands
{
    public class PHA : PushCommand
    {
        public PHA() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.PushStack(processor.Acc);
        }
    }
}