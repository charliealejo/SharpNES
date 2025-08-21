namespace Ricoh6502.Commands
{
    public class CLV : ImpliedCommand
    {
        public CLV() : base()
        {
        }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.Status.OverflowFlag = false;
        }
    }
}
