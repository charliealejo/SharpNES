
namespace Ricoh6502.Commands
{
    public class BreakCommand : Command
    {
        protected bool SoftwareInterrupt { get; }

        public BreakCommand(bool softwareInterrupt) : base(AddressingMode.Implied)
        {
            SoftwareInterrupt = softwareInterrupt;
        }

        protected override void ExecuteInternal(CPU cpu)
        {
            var returnAddress = (ushort)(cpu.PC + 2);
            cpu.PushStack((byte)(returnAddress >> 8));
            cpu.PushStack((byte)returnAddress);
            cpu.PushStack(cpu.Status.GetStatus());
            cpu.Status.InterruptDisable = true;
            cpu.Status.BreakCommand = SoftwareInterrupt;
            cpu.SetPCWithInterruptVector(0xFFFE);
        }

        protected override byte GetInstructionCycleCount()
        {
            return 7;
        }

        protected override ushort GetNextInstructionAddress(CPU cpu)
        {
            return cpu.PC;
        }

        protected override bool CheckForPageBoundaryCrossing(ushort currentInstructionAddress, ushort nextInstructionAddress)
        {
            return false;
        }
    }
}