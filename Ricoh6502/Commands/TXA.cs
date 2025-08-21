namespace Ricoh6502.Commands
{
    public class TXA : ImpliedCommand
    {
        public TXA() : base() { }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.Acc = cpu.X;
            cpu.Status.SetZeroAndNegativeFlags(cpu.Acc);
        }
    }
}