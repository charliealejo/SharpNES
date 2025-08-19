namespace Ricoh6502.Commands
{
    public class TAY : ImpliedCommand
    {
        public TAY() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Y = processor.Acc;
            processor.Status.SetZeroAndNegativeFlags(processor.Y);
        }
    }
}