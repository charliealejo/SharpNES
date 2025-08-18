namespace Ricoh6502.Commands
{
    public class INX : ImpliedCommand
    {
        public INX() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.X++;
            processor.Status.SetZeroAndNegativeFlags(processor.X);
        }
    }
}