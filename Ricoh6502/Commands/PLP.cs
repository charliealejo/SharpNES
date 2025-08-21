namespace Ricoh6502.Commands
{
    public class PLP : PullCommand
    {
        public PLP() : base() { }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.Status.SetStatus(cpu.PopStack());
        }
    }
}