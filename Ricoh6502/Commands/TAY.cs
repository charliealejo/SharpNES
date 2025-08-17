namespace Ricoh6502.Commands
{
    public abstract class TAY : TBase
    {
        public TAY() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Y = processor.Acc;
        }
    }
}