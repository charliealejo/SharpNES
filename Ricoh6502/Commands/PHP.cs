namespace Ricoh6502.Commands
{
    public class PHP : PushCommand
    {
        public PHP() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.PushStack(processor.Status.GetStatus());
        }
    }
}