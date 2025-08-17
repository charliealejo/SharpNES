namespace Ricoh6502.Commands
{
    public abstract class TAY : ImpliedCommand
    {
        public TAY() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Y = processor.Acc;
        }
    }
}