namespace Ricoh6502.Commands
{
    public class JMP : Command
    {
        public JMP(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode)
        {
            D1 = d1;
            D2 = d2;
        }

        protected override void ExecuteInternal(Processor processor)
        {
            var address = BitConverter.ToUInt16([D1, D2], 0);
            processor.PC = AddressingMode == AddressingMode.Absolute
                ? address
                : BitConverter.ToUInt16(
                    [processor.Memory[address],
                     (address & 0x00FF) == 0xFF
                        ? processor.Memory[address & 0xFF00]
                        : processor.Memory[address + 1]], 0);
        }

        protected override byte GetInstructionCycleCount()
        {
            return AddressingMode == AddressingMode.Absolute ? (byte)3 : (byte)5;
        }

        protected override ushort GetNextInstructionAddress(Processor processor)
        {
            return processor.PC;
        }

        protected override bool CheckForPageBoundaryCrossing(ushort currentInstructionAddress, ushort nextInstructionAddress)
        {
            return false;
        }
    }
}