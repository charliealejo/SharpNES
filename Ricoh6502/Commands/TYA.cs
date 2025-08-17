namespace Ricoh6502.Commands
{
    public abstract class TYA : TBase
    {
        public TYA() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Acc = processor.Y;
        }
    }
}