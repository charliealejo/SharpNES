namespace Ricoh6502.Commands
{
    public class PHP : PushCommand
    {
        public PHP() : base() { }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.PushStack(cpu.Status.GetStatus());
        }
    }
}