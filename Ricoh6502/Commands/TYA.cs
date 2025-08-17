namespace Ricoh6502.Commands
{
    public abstract class TYA : ImpliedCommand
    {
        public TYA() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Acc = processor.Y;
        }
    }
}