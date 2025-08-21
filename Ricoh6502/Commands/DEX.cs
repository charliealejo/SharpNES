namespace Ricoh6502.Commands
{
    public class DEX : ImpliedCommand
    {
        public DEX() : base() { }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.X--;
            cpu.Status.SetZeroAndNegativeFlags(cpu.X);
        }
    }
}