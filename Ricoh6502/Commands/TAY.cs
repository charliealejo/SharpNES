namespace Ricoh6502.Commands
{
    public class TAY : ImpliedCommand
    {
        public TAY() : base() { }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.Y = cpu.Acc;
            cpu.Status.SetZeroAndNegativeFlags(cpu.Y);
        }
    }
}