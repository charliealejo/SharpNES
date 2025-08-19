namespace Ricoh6502.Commands
{
    public class TYA : ImpliedCommand
    {
        public TYA() : base() { }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.Acc = processor.Y;
            processor.Status.SetZeroAndNegativeFlags(processor.Acc);
        }
    }
}