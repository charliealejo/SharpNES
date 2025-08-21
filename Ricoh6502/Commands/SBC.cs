namespace Ricoh6502.Commands
{
    public class SBC : BoundaryCheckCommand
    {
        public SBC(AddressingMode addressingMode, byte d1, byte d2) : base(addressingMode, d1, d2) { }

        protected override void ExecuteInternal(CPU cpu)
        {
            byte prevAcc = cpu.Acc;
            byte value = (byte)(~cpu.GetValue(AddressingMode, D1, D2) & 0xFF);
            byte carry = cpu.Status.CarryFlag ? (byte)1 : (byte)0;
            var result = cpu.Acc + value + carry;
            cpu.Acc = (byte)(result & 0xFF);
            cpu.Status.CarryFlag = !((sbyte)cpu.Acc < 0);
            cpu.Status.OverflowFlag = ((cpu.Acc ^ prevAcc) & (cpu.Acc ^ value) & 0x80) != 0;
            cpu.Status.SetZeroAndNegativeFlags(cpu.Acc);
        }
    }
}