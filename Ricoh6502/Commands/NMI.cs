
namespace Ricoh6502.Commands
{
    public class NMI : Command
    {
        public NMI() : base(AddressingMode.Implied)
        {
        }

        protected override void ExecuteInternal(CPU cpu)
        {
            var returnAddress = cpu.PC;
            cpu.PushStack((byte)(returnAddress >> 8));
            cpu.PushStack((byte)returnAddress);
            cpu.PushStack(cpu.Status.GetStatus());
            cpu.Status.InterruptDisable = true;
            cpu.SetPCWithInterruptVector(0xFFFA);
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