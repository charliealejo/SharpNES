namespace Ricoh6502.Commands
{
    public class INX : ImpliedCommand
    {
        public INX() : base() { }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.X++;
            cpu.Status.SetZeroAndNegativeFlags(cpu.X);
        }
    }
}