namespace Ricoh6502.Commands
{
    public class CLC : ImpliedCommand
    {
        public CLC() : base()
        {
        }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.Status.CarryFlag = false;
        }
    }
}
