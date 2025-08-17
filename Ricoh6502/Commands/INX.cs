namespace Ricoh6502.Commands
{
    public abstract class INX : ImpliedCommandBase
    {
        public INX() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.X++;
            processor.Status.SetZeroAndNegativeFlags(processor.X);
        }
    }
}