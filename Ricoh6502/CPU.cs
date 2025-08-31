using System.Net;
using Ricoh6502.Commands;

namespace Ricoh6502
{
    public class CPU
    {
        private bool _interrupt;

        private bool _nonMaskableInterrupt;

        private bool _executeDMA;

        private uint _dmaCycle;

        private uint _lastDmaCycle;

        private uint _nextInstructionCycle;

        public uint Cycles { get; private set; }

        public event EventHandler<MemoryAccessEventArgs>? PPURegisterAccessed;

        public event EventHandler<MemoryAccessEventArgs>? DMAWrite;

        public event EventHandler<bool>? InputDevicePolling;

        public CPU()
        {
            _interrupt = false;
            _nonMaskableInterrupt = false;
            _executeDMA = false;
            _dmaCycle = 0;
            _lastDmaCycle = 0;
        }

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
        public byte SP { get; set; } = 0;

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

        /// <summary>
        /// Code that executes every tick of the clock
        /// </summary>
        /// <returns>true if the CPU is still executing, false if it has halted</returns>
        public bool Clock()
        {
            if (!IsCycleExecutingCommand())
            {
                Cycles++;
                return true;
            }

            if (_executeDMA)
            {
                PerformDMA();
                return true;
            }

            // Fetch the next instruction
            byte opcode = Memory[PC];
            byte d1 = Memory[(ushort)(PC + 1)];
            byte d2 = Memory[(ushort)(PC + 2)];

            // Create the next instruction checking for interrupts
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

            // Check for break command
            if (command is BRK)
            {
                return false;
            }

            // Execute the instruction
            _nextInstructionCycle += command.Execute(this);

            // Clear the interrupt flags
            if (command is IRQ)
            {
                _interrupt = false;
            }
            else if (_nonMaskableInterrupt)
            {
                _nonMaskableInterrupt = false;
            }

            Cycles++;
            return true;
        }

        public bool IsCycleExecutingCommand()
        {
            return Cycles == _nextInstructionCycle;
        }

        public void IRQ()
        {
            _interrupt = true;
        }

        public void NMI()
        {
            _nonMaskableInterrupt = true;
        }

        public ushort GetBaseAddress(AddressingMode addressingMode, byte d1, byte d2)
        {
            return addressingMode switch
            {
                AddressingMode.AbsoluteX => BitConverter.ToUInt16([d1, d2], 0),
                AddressingMode.AbsoluteY => BitConverter.ToUInt16([d1, d2], 0),
                AddressingMode.IndirectY => BitConverter.ToUInt16([Memory[d1], Memory[(byte)(d1 + 1)]], 0),
                _ => 0,
            };
        }

        public ushort GetEffectiveAddress(AddressingMode addressingMode, byte d1, byte d2)
        {
            return addressingMode switch
            {
                AddressingMode.Implied => PC,
                AddressingMode.Immediate => PC,
                AddressingMode.Accumulator => PC,
                AddressingMode.ZeroPage => (ushort)(d1 & 0x00FF),
                AddressingMode.ZeroPageX => (byte)(d1 + X),
                AddressingMode.ZeroPageY => (byte)(d1 + Y),
                AddressingMode.Relative => (byte)(PC + (sbyte)d1),
                AddressingMode.Absolute => BitConverter.ToUInt16([d1, d2], 0),
                AddressingMode.AbsoluteX => (ushort)(BitConverter.ToUInt16([d1, d2], 0) + X),
                AddressingMode.AbsoluteY => (ushort)(BitConverter.ToUInt16([d1, d2], 0) + Y),
                AddressingMode.Indirect => Memory[BitConverter.ToUInt16([d1, d2], 0)],
                AddressingMode.IndirectX => BitConverter.ToUInt16([Memory[(byte)(d1 + X)], Memory[(byte)(d1 + X + 1)]], 0),
                AddressingMode.IndirectY => (ushort)(BitConverter.ToUInt16([Memory[d1], Memory[(byte)(d1 + 1)]], 0) + Y),
                _ => throw new ArgumentOutOfRangeException(nameof(addressingMode), addressingMode, null),
            };
        }

        public byte GetValue(AddressingMode addressingMode, byte d1, byte d2)
        {
            if (addressingMode == AddressingMode.Immediate)
            {
                return d1;
            }
            else if (addressingMode == AddressingMode.Accumulator)
            {
                return Acc;
            }

            var memoryAddress = GetEffectiveAddress(addressingMode, d1, d2);
            if (addressingMode == AddressingMode.Indirect)
            {
                memoryAddress = GetIndirectMemoryAddress(d1, d2);
            }

            return Memory[memoryAddress];
        }

        public void SetValue(AddressingMode addressingMode, byte d1, byte d2, byte value)
        {
            if (addressingMode == AddressingMode.Accumulator)
            {
                Acc = value;
            }

            var memoryAddress = GetEffectiveAddress(addressingMode, d1, d2);
            if (addressingMode == AddressingMode.Indirect)
            {
                memoryAddress = GetIndirectMemoryAddress(d1, d2);
            }

            if (memoryAddress >= 0x2000 && memoryAddress < 0x4000)
            {
                PPURegisterAccessed?.Invoke(this, new MemoryAccessEventArgs((uint)(memoryAddress % 8), value));
                for (ushort addr = 0x2000; addr <= 0x3FFF; addr += 8)
                {
                    Memory[addr] = value;
                }
            }
            else if (memoryAddress == 0x4014)
            {
                PPURegisterAccessed?.Invoke(this, new MemoryAccessEventArgs(0x14, value));
                _executeDMA = true;
            }
            else if (memoryAddress == 0x4016)
            {
                InputDevicePolling?.Invoke(this, (value & 0x01) != 0);
            }

            Memory[memoryAddress] = value;
        }

        public void SetPCWithInterruptVector(ushort irqAddress)
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
            SetPCWithInterruptVector(0xFFFC);
            SP -= 3;
            Status.InterruptDisable = true;
            Cycles = 0;
            _nextInstructionCycle = 7;
            _executeDMA = false;
            _dmaCycle = 0;
            _lastDmaCycle = 0;
        }

        private void PerformDMA()
        {
            if (_lastDmaCycle == 0)
            {
                _lastDmaCycle = Cycles % 2 == 0 ? 514 : (uint)513;
            }

            if (_dmaCycle >= 512)
            {
                _nextInstructionCycle += _lastDmaCycle - _dmaCycle;
                _lastDmaCycle = 0;
                _dmaCycle = 0;
                _executeDMA = false;
            }
            else if (_dmaCycle % 2 == 0)
            {
                var page = Memory[0x4014];
                uint offset = _dmaCycle / 2;
                DMAWrite?.Invoke(this, new MemoryAccessEventArgs(offset, Memory[(ushort)(page * 0x100 + offset)]));
                _nextInstructionCycle += 2;
                _dmaCycle++;
            }

            Cycles++;
        }

        private ushort GetIndirectMemoryAddress(byte d1, byte d2)
        {
            return BitConverter.ToUInt16(
                [Memory[BitConverter.ToUInt16([d1, d2], 0)],
                     (BitConverter.ToUInt16([d1, d2], 0) & 0x00FF) == 0xFF
                        ? Memory[BitConverter.ToUInt16([d1, d2], 0) & 0xFF00]
                        : Memory[BitConverter.ToUInt16([d1, d2], 0) + 1]], 0);
        }
    }
}
