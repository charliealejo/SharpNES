using Ricoh6502.Commands;

namespace Ricoh6502
{
    public class Processor()
    {
        /// <summary>
        /// Gets the memory array representing the processor's addressable memory space.
        /// </summary>
        public byte[] Memory { get; } = new byte[65536];

        /// <summary>
        /// Gets or sets the program counter (PC), which represents the current execution address in memory.
        /// </summary>
        public ushort PC { get; set; }

        /// <summary>
        /// Gets or sets the stack pointer value.
        /// </summary>
        public byte SP { get; set; } = 0xFF;

        /// <summary>
        /// Gets or sets the accumulated value as an 8-bit unsigned integer.
        /// </summary>
        public byte Acc { get; set; }

        /// <summary>
        /// Gets or sets the value of the X register as an 8-bit unsigned integer.
        /// </summary>
        public byte X { get; set; }

        /// <summary>
        /// Gets or sets the value of the Y register as an 8-bit unsigned integer.
        /// </summary>
        public byte Y { get; set; }

        /// <summary>
        /// Gets the status value represented as a byte.
        /// </summary>
        public Status Status { get; } = new Status();

        public byte GetValue(AddressingMode addressingMode, byte d1, byte d2)
        {
            return addressingMode switch
            {
                AddressingMode.Immediate => d1,
                AddressingMode.Accumulator => Acc,
                AddressingMode.ZeroPage => Memory[d1],
                AddressingMode.ZeroPageX => Memory[(byte)(d1 + X)],
                AddressingMode.ZeroPageY => Memory[(byte)(d1 + Y)],
                AddressingMode.Relative => Memory[(byte)(PC + (sbyte)d1)],
                AddressingMode.Absolute => Memory[BitConverter.ToUInt16([d2, d1], 0)],
                AddressingMode.AbsoluteX => Memory[BitConverter.ToUInt16([d2, d1], 0) + X],
                AddressingMode.AbsoluteY => Memory[BitConverter.ToUInt16([d2, d1], 0) + Y],
                AddressingMode.Indirect => Memory[BitConverter.ToUInt16([Memory[d2], Memory[d1]], 0)],
                AddressingMode.IndirectX => Memory[BitConverter.ToUInt16([Memory[(byte)(d1 + X)], Memory[(byte)(d1 + X + 1)]], 0)],
                AddressingMode.IndirectY => Memory[BitConverter.ToUInt16([Memory[d2], Memory[d2 + 1]], 0) + Y],
                _ => throw new ArgumentOutOfRangeException(nameof(addressingMode), addressingMode, null),
            };
        }

        public void SetValue(AddressingMode addressingMode, byte d1, byte d2, byte value)
        {
            switch (addressingMode)
            {
                case AddressingMode.ZeroPage:
                    Memory[d1] = value;
                    break;
                case AddressingMode.ZeroPageX:
                    Memory[(byte)(d1 + X)] = value;
                    break;
                case AddressingMode.ZeroPageY:
                    Memory[(byte)(d1 + Y)] = value;
                    break;
                case AddressingMode.Relative:
                    Memory[(byte)(PC + (sbyte)d1)] = value;
                    break;
                case AddressingMode.Absolute:
                    Memory[BitConverter.ToUInt16([d2, d1], 0)] = value;
                    break;
                case AddressingMode.AbsoluteX:
                    Memory[BitConverter.ToUInt16([d2, d1], 0) + X] = value;
                    break;
                case AddressingMode.AbsoluteY:
                    Memory[BitConverter.ToUInt16([d2, d1], 0) + Y] = value;
                    break;
                case AddressingMode.Indirect:
                    Memory[BitConverter.ToUInt16([Memory[d2], Memory[d1]], 0)] = value;
                    break;
                case AddressingMode.IndirectX:
                    Memory[BitConverter.ToUInt16([Memory[(byte)(d1 + X)], Memory[(byte)(d1 + X + 1)]], 0)] = value;
                    break;
                case AddressingMode.IndirectY:
                    Memory[BitConverter.ToUInt16([Memory[d2], Memory[d2 + 1]], 0) + Y] = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(addressingMode), addressingMode, null);
            }
        }

        public void PushStack(byte v)
        {
            Memory[0x0100 | SP] = v;
            SP--;
        }

        public byte PopStack()
        {
            SP++;
            return Memory[0x0100 | SP];
        }
    }
}
