namespace Ricoh6502.Commands
{
    public abstract class INY : ImpliedCommandBase
    {
        public INY() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Y++;
            processor.Status.SetZeroAndNegativeFlags(processor.Y);
        }
    }
}