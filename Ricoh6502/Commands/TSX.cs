namespace Ricoh6502.Commands
{
    public class TSX : ImpliedCommand
    {
        public TSX() : base() { }

        protected override void ExecuteInternal(CPU cpu)
        {
            cpu.X = cpu.SP;
            cpu.Status.SetZeroAndNegativeFlags(cpu.X);
        }
    }
}