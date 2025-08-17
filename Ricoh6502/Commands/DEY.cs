namespace Ricoh6502.Commands
{
    public abstract class DEY : ImpliedCommand
    {
        public DEY() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Y--;
            processor.Status.SetZeroAndNegativeFlags(processor.Y);
        }
    }
}