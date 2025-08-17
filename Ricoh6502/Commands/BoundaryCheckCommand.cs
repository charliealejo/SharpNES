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

        protected override byte GetInstructionCycleCount()
        {
            return AddressingMode switch
            {
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

        protected override ushort GetNextInstructionAddress(Processor processor)
        {
            return AddressingMode switch
            {
                AddressingMode.Immediate => (ushort)(processor.PC + 2),
                AddressingMode.ZeroPage => (ushort)(processor.PC + 2),
                AddressingMode.ZeroPageX => (ushort)(processor.PC + 2),
                AddressingMode.ZeroPageY => (ushort)(processor.PC + 2),
                AddressingMode.Absolute => (ushort)(processor.PC + 3),
                AddressingMode.AbsoluteX => (ushort)(processor.PC + 3),
                AddressingMode.AbsoluteY => (ushort)(processor.PC + 3),
                AddressingMode.IndirectX => (ushort)(processor.PC + 2),
                AddressingMode.IndirectY => (ushort)(processor.PC + 2),
                _ => throw new ArgumentOutOfRangeException(nameof(AddressingMode), AddressingMode, null)
            };
        }

        protected override bool CheckForPageBoundaryCrossing(ushort currentInstructionAddress, ushort nextInstructionAddress)
        {
            if (AddressingMode == AddressingMode.AbsoluteX ||
                AddressingMode == AddressingMode.AbsoluteY ||
                AddressingMode == AddressingMode.IndirectY)
            {
                return (nextInstructionAddress & 0xFF00) != (currentInstructionAddress & 0xFF00);
            }
            return false;
        }
    }
}