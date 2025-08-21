namespace Ricoh6502.Commands
{
    public abstract class BoundaryCheckCommand : Command
    {
        public BoundaryCheckCommand(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode)
        {
            AddressingMode = addressingMode;
            D1 = d1;
            D2 = d2;
        }

        public override byte Execute(CPU cpu)
        {
            PageCrossed = CheckForPageBoundaryCrossing(
                cpu.GetBaseAddress(AddressingMode, D1, D2),
                cpu.GetEffectiveAddress(AddressingMode, D1, D2));
            return base.Execute(cpu);
        }

        protected override byte GetInstructionCycleCount()
        {
            return AddressingMode switch
            {
                AddressingMode.Implied => 2,
                AddressingMode.Immediate => 2,
                AddressingMode.ZeroPage => 3,
                AddressingMode.ZeroPageX => 4,
                AddressingMode.ZeroPageY => 4,
                AddressingMode.Absolute => 4,
                AddressingMode.AbsoluteX => 4,
                AddressingMode.AbsoluteY => 4,
                AddressingMode.IndirectX => 6,
                AddressingMode.IndirectY => 5,
                _ => throw new ArgumentOutOfRangeException(nameof(AddressingMode), AddressingMode, null)
            };
        }

        protected override ushort GetNextInstructionAddress(CPU cpu)
        {
            return AddressingMode switch
            {
                AddressingMode.Implied => (ushort)(cpu.PC + 1),
                AddressingMode.Immediate => (ushort)(cpu.PC + 2),
                AddressingMode.ZeroPage => (ushort)(cpu.PC + 2),
                AddressingMode.ZeroPageX => (ushort)(cpu.PC + 2),
                AddressingMode.ZeroPageY => (ushort)(cpu.PC + 2),
                AddressingMode.Absolute => (ushort)(cpu.PC + 3),
                AddressingMode.AbsoluteX => (ushort)(cpu.PC + 3),
                AddressingMode.AbsoluteY => (ushort)(cpu.PC + 3),
                AddressingMode.IndirectX => (ushort)(cpu.PC + 2),
                AddressingMode.IndirectY => (ushort)(cpu.PC + 2),
                _ => throw new ArgumentOutOfRangeException(nameof(AddressingMode), AddressingMode, null)
            };
        }

        protected override bool CheckForPageBoundaryCrossing(ushort baseAddress, ushort effectiveAddress)
        {
            if (AddressingMode == AddressingMode.AbsoluteX ||
                AddressingMode == AddressingMode.AbsoluteY ||
                AddressingMode == AddressingMode.IndirectY)
            {
                return (baseAddress & 0xFF00) != (effectiveAddress & 0xFF00);
            }
            return false;
        }
    }
}