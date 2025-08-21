namespace Ricoh6502.Commands
{
    public class SEI : ImpliedCommand
    {
        public SEI() : base()
        {
        }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.Status.InterruptDisable = true;
        }
    }
}
