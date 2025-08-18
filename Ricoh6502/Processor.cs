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
        public ushort PC { get; set; } = 0xFFFC;

        /// <summary>
        /// Gets or sets the stack pointer value.
        /// </summary>
        public byte SP { get; set; } = 0xFD;

        /// <summary>
        /// Gets or sets the accumulated value as an 8-bit unsigned integer.
        /// </summary>
        public byte Acc { get; set; } = 0;

        /// <summary>
        /// Gets or sets the value of the X register as an 8-bit unsigned integer.
        /// </summary>
        public byte X { get; set; } = 0;

        /// <summary>
        /// Gets or sets the value of the Y register as an 8-bit unsigned integer.
        /// </summary>
        public byte Y { get; set; } = 0;

        /// <summary>
        /// Gets the status value represented as a byte.
        /// </summary>
        public Status Status { get; } = new Status();

        private bool _interrupt = false;

        private bool _nonMaskableInterrupt = false;

        public void Run()
        {
            while (true)
            {
                // Fetch the next instruction
                byte opcode = Memory[PC];
                byte d1 = Memory[(ushort)(PC + 1)];
                byte d2 = Memory[(ushort)(PC + 2)];

                // Decode the instruction
                Command command;
                if (_nonMaskableInterrupt)
                {
                    command = new NMI();
                }
                else if (_interrupt && !Status.InterruptDisable)
                {
                    command = new IRQ();
                }
                else
                {
                    command = CommandFactory.CreateCommand(opcode, d1, d2);
                }

                // Execute the instruction
                command.Execute(this);

                // Clear the interrupt flags
                if (command is IRQ)
                {
                    _interrupt = false;
                }
                else if (_nonMaskableInterrupt)
                {
                    _nonMaskableInterrupt = false;
                }
            }
        }

        public void IRQ()
        {
            _interrupt = true;
        }

        public void NMI()
        {
            _nonMaskableInterrupt = true;
        }

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

        public void SetPCForInterrupt(ushort irqAddress)
        {
            PC = (ushort)(Memory[irqAddress] | (Memory[irqAddress + 1] << 8));
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

        public void Reset()
        {
            PC = 0xFFFC;
            SP = SP -= 3;
            Status.InterruptDisable = true;
        }
    }
}
