namespace Ricoh6502.Commands
{
    public class PHA : PushCommand
    {
        public PHA() : base() { }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.PushStack(cpu.Acc);
        }
    }
}