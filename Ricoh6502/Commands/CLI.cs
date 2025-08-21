namespace Ricoh6502.Commands
{
    public class CLI : ImpliedCommand
    {
        public CLI() : base()
        {
        }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.Status.InterruptDisable = false;
        }
    }
}
