namespace Ricoh6502.Commands
{
    public class INY : ImpliedCommand
    {
        public INY() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Y++;
            processor.Status.SetZeroAndNegativeFlags(processor.Y);
        }
    }
}