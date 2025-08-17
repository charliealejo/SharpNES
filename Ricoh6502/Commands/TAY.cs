namespace Ricoh6502.Commands
{
    public abstract class TAY : ImpliedCommandBase
    {
        public TAY() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Y = processor.Acc;
        }
    }
}