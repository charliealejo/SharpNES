namespace Ricoh6502.Commands
{
    public class TAX : ImpliedCommand
    {
        public TAX() : base() { }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.X = cpu.Acc;
            cpu.Status.SetZeroAndNegativeFlags(cpu.X);
        }
    }
}