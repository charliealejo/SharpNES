namespace Ricoh6502.Commands
{
    public class INY : ImpliedCommand
    {
        public INY() : base() { }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.Y++;
            cpu.Status.SetZeroAndNegativeFlags(cpu.Y);
        }
    }
}