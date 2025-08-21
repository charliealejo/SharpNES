namespace Ricoh6502.Commands
{
    public abstract class StoreCommand : Command
    {
        public StoreCommand(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode)
        {
            AddressingMode = addressingMode;
            D1 = d1;
            D2 = d2;
        }

        protected override byte GetInstructionCycleCount()
        {
            return AddressingMode switch
            {
                AddressingMode.ZeroPage => 3,
                AddressingMode.ZeroPageX => 4,
                AddressingMode.ZeroPageY => 4,
                AddressingMode.Absolute => 4,
                AddressingMode.AbsoluteX => 5,
                AddressingMode.AbsoluteY => 5,
                AddressingMode.IndirectX => 6,
                AddressingMode.IndirectY => 6,
                _ => throw new ArgumentOutOfRangeException(nameof(AddressingMode), AddressingMode, null),
            };
        }

        protected override ushort GetNextInstructionAddress(CPU cpu)
        {
            return AddressingMode switch
            {
                AddressingMode.ZeroPage => (ushort)(cpu.PC + 2),
                AddressingMode.ZeroPageX => (ushort)(cpu.PC + 2),
                AddressingMode.ZeroPageY => (ushort)(cpu.PC + 2),
                AddressingMode.Absolute => (ushort)(cpu.PC + 3),
                AddressingMode.AbsoluteX => (ushort)(cpu.PC + 3),
                AddressingMode.AbsoluteY => (ushort)(cpu.PC + 3),
                AddressingMode.IndirectX => (ushort)(cpu.PC + 2),
                AddressingMode.IndirectY => (ushort)(cpu.PC + 2),
                _ => throw new ArgumentOutOfRangeException(nameof(AddressingMode), AddressingMode, null),
            };
        }

        protected override bool CheckForPageBoundaryCrossing(ushort currentInstructionAddress, ushort nextInstructionAddress)
        {
            return false;
        }
    }
}