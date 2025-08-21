namespace Ricoh6502.Commands
{
    public class SED : ImpliedCommand
    {
        public SED() : base()
        {
        }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.Status.DecimalMode = true;
        }
    }
}
