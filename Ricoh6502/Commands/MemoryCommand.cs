namespace Ricoh6502.Commands
{
    public abstract class MemoryCommand : Command
    {
        protected MemoryCommand(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode)
        {
            AddressingMode = addressingMode;
            D1 = d1;
            D2 = d2;
        }

        protected override byte GetInstructionCycleCount()
        {
            return AddressingMode switch
            {
                AddressingMode.Accumulator => 2,
                AddressingMode.ZeroPage => 5,
                AddressingMode.ZeroPageX => 6,
                AddressingMode.Absolute => 6,
                AddressingMode.AbsoluteX => 7,
                _ => throw new ArgumentOutOfRangeException(nameof(AddressingMode), AddressingMode, null)
            };
        }

        protected override ushort GetNextInstructionAddress(CPU cpu)
        {
            return AddressingMode switch
            {
                AddressingMode.Accumulator => (ushort)(cpu.PC + 1),
                AddressingMode.ZeroPage => (ushort)(cpu.PC + 2),
                AddressingMode.ZeroPageX => (ushort)(cpu.PC + 2),
                AddressingMode.Absolute => (ushort)(cpu.PC + 3),
                AddressingMode.AbsoluteX => (ushort)(cpu.PC + 3),
                _ => throw new ArgumentOutOfRangeException(nameof(AddressingMode), AddressingMode, null)
            };
        }

        protected override bool CheckForPageBoundaryCrossing(ushort currentInstructionAddress, ushort nextInstructionAddress)
        {
            return false;
        }
    }
}