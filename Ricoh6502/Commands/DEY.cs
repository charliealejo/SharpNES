namespace Ricoh6502.Commands
{
    public class DEY : ImpliedCommand
    {
        public DEY() : base() { }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.Y--;
            cpu.Status.SetZeroAndNegativeFlags(cpu.Y);
        }
    }
}