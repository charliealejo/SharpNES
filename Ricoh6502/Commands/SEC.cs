namespace Ricoh6502.Commands
{
    public class SEC : ImpliedCommand
    {
        public SEC() : base()
        {
        }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.Status.CarryFlag = true;
        }
    }
}
