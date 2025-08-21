namespace Ricoh6502.Commands
{
    public class TYA : ImpliedCommand
    {
        public TYA() : base() { }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.Acc = cpu.Y;
            cpu.Status.SetZeroAndNegativeFlags(cpu.Acc);
        }
    }
}