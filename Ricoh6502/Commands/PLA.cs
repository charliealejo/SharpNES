namespace Ricoh6502.Commands
{
    public class PLA : PullCommand
    {
        public PLA() : base() { }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.Acc = cpu.PopStack();
            cpu.Status.SetZeroAndNegativeFlags(cpu.Acc);
        }
    }
}