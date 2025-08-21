namespace Ricoh6502.Commands
{
    public class JMP : Command
    {
        public JMP(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode)
        {
            D1 = d1;
            D2 = d2;
        }

        protected override void ExecuteInternal(CPU cpu)
        {
            var address = BitConverter.ToUInt16([D1, D2], 0);
            cpu.PC = AddressingMode == AddressingMode.Absolute
                ? address
                : BitConverter.ToUInt16(
                    [cpu.Memory[address],
                     (address & 0x00FF) == 0xFF
                        ? cpu.Memory[address & 0xFF00]
                        : cpu.Memory[address + 1]], 0);
        }

        protected override byte GetInstructionCycleCount()
        {
            return AddressingMode == AddressingMode.Absolute ? (byte)3 : (byte)5;
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