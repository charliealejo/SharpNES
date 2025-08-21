namespace Ricoh6502.Commands
{
    public class CLD : ImpliedCommand
    {
        public CLD() : base()
        {
        }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.Status.DecimalMode = false;
        }
    }
}
