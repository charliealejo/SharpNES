namespace Ricoh6502.Commands
{
    public class STA : Command
    {
        public STA(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode)
        {
            AddressingMode = addressingMode;
            D1 = d1;
            D2 = d2;
        }

        protected override void ExecuteInternal(Processor processor)
        {
            processor.SetValue(AddressingMode, D1, D2, processor.Acc);
        }

        protected override byte GetInstructionCycleCount()
        {
            return AddressingMode switch
            {
                AddressingMode.ZeroPage => 3,
                AddressingMode.ZeroPageX => 4,
                AddressingMode.Absolute => 4,
                AddressingMode.AbsoluteX => 5,
                AddressingMode.AbsoluteY => 5,
                AddressingMode.IndirectX => 6,
                AddressingMode.IndirectY => 6,
                _ => throw new ArgumentOutOfRangeException(nameof(AddressingMode), AddressingMode, null),
            };
        }

        protected override ushort GetNextInstructionAddress(Processor processor)
        {
            return AddressingMode switch
            {
                AddressingMode.ZeroPage => (ushort)(processor.PC + 2),
                AddressingMode.ZeroPageX => (ushort)(processor.PC + 2),
                AddressingMode.Absolute => (ushort)(processor.PC + 3),
                AddressingMode.AbsoluteX => (ushort)(processor.PC + 3),
                AddressingMode.AbsoluteY => (ushort)(processor.PC + 3),
                AddressingMode.IndirectX => (ushort)(processor.PC + 2),
                AddressingMode.IndirectY => (ushort)(processor.PC + 2),
                _ => throw new ArgumentOutOfRangeException(nameof(AddressingMode), AddressingMode, null),
            };
        }

        protected override bool CheckForPageBoundaryCrossing(ushort currentInstructionAddress, ushort nextInstructionAddress)
        {
            return false;
        }
    }
}