namespace Ricoh6502.Commands
{
    public abstract class DEY : ImpliedCommandBase
    {
        public DEY() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Y--;
            processor.Status.SetZeroAndNegativeFlags(processor.Y);
        }
    }
}