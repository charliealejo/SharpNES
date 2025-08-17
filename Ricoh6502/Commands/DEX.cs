namespace Ricoh6502.Commands
{
    public abstract class DEX : ImpliedCommandBase
    {
        public DEX() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.X--;
            processor.Status.SetZeroAndNegativeFlags(processor.X);
        }
    }
}