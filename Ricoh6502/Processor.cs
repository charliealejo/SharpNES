using System.Diagnostics;
using Ricoh6502.Commands;

namespace Ricoh6502
{
    public class Processor
    {
        private readonly bool _debug;

        private StreamWriter? _logWriter;

        private double _cpuCycleTimeInNanoSeconds;

        private double _cpuFrequency;

        private bool _interrupt;

        private bool _nonMaskableInterrupt;

        private uint _cycles;

        public Processor(SystemVersion version, bool debug = false)
        {
            _cpuFrequency = version switch
            {
                SystemVersion.NTSC => 1_789_773,
                SystemVersion.PAL => 1_662_607,
                SystemVersion.Dendy => 1_773_448,
                _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
            };
            _cpuCycleTimeInNanoSeconds = 1e9 / _cpuFrequency;

            _debug = debug;
            if (_debug)
            {
                var logDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!System.IO.Directory.Exists(logDir))
                    System.IO.Directory.CreateDirectory(logDir);
                var logPath = System.IO.Path.Combine(logDir, $"cpu_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                _logWriter = new System.IO.StreamWriter(logPath, false) { AutoFlush = true };
            }

            _interrupt = false;
            _nonMaskableInterrupt = false;
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

        public void Run(ushort startPC = 0)
        {
            if (startPC == 0)
            {
                SetPCWithInterruptVector(0xFFFC);
            }
            else
            {
                PC = startPC;
            }

            _cycles = 7;
            var clock = new Stopwatch();
            var spinWait = new SpinWait();
            while (true)
            {
                clock.Restart();

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

                // Debug logging before execution
                if (_debug && _logWriter != null)
                {
                    WriteLog(opcode, d1, d2, command);
                }
                if (command is BRK)
                {
                    break;
                }

                // Execute the instruction
                var cycles = command.Execute(this);

                // Clear the interrupt flags
                if (command is IRQ)
                {
                    _interrupt = false;
                }
                else if (_nonMaskableInterrupt)
                {
                    _nonMaskableInterrupt = false;
                }

                // Wait if needed
                do
                {
                    spinWait.SpinOnce();
                } while (clock.Elapsed.TotalNanoseconds < cycles * _cpuCycleTimeInNanoSeconds);

                _cycles += cycles;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _logWriter != null)
            {
                _logWriter.Dispose();
                _logWriter = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
            return addressingMode switch
            {
                AddressingMode.Immediate => d1,
                AddressingMode.Accumulator => Acc,
                AddressingMode.ZeroPage => Memory[d1],
                AddressingMode.ZeroPageX => Memory[(byte)(d1 + X)],
                AddressingMode.ZeroPageY => Memory[(byte)(d1 + Y)],
                AddressingMode.Relative => Memory[(byte)(PC + (sbyte)d1)],
                AddressingMode.Absolute => Memory[BitConverter.ToUInt16([d1, d2], 0)],
                AddressingMode.AbsoluteX => Memory[(ushort)(BitConverter.ToUInt16([d1, d2], 0) + X)],
                AddressingMode.AbsoluteY => Memory[(ushort)(BitConverter.ToUInt16([d1, d2], 0) + Y)],
                AddressingMode.Indirect => Memory[BitConverter.ToUInt16(
                    [Memory[BitConverter.ToUInt16([d1, d2], 0)],
                     (BitConverter.ToUInt16([d1, d2], 0) & 0x00FF) == 0xFF
                        ? Memory[BitConverter.ToUInt16([d1, d2], 0) & 0xFF00]
                        : Memory[BitConverter.ToUInt16([d1, d2], 0) + 1]], 0)],
                AddressingMode.IndirectX => Memory[BitConverter.ToUInt16([Memory[(byte)(d1 + X)], Memory[(byte)(d1 + X + 1)]], 0)],
                AddressingMode.IndirectY => Memory[(ushort)(BitConverter.ToUInt16([Memory[d1], Memory[(byte)(d1 + 1)]], 0) + Y)],
                _ => throw new ArgumentOutOfRangeException(nameof(addressingMode), addressingMode, null),
            };
        }

        public void SetValue(AddressingMode addressingMode, byte d1, byte d2, byte value)
        {
            switch (addressingMode)
            {
                case AddressingMode.Accumulator:
                    Acc = value;
                    break;
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
                    Memory[BitConverter.ToUInt16([d1, d2], 0)] = value;
                    break;
                case AddressingMode.AbsoluteX:
                    Memory[BitConverter.ToUInt16([d1, d2], 0) + X] = value;
                    break;
                case AddressingMode.AbsoluteY:
                    Memory[BitConverter.ToUInt16([d1, d2], 0) + Y] = value;
                    break;
                case AddressingMode.IndirectX:
                    Memory[BitConverter.ToUInt16([Memory[(byte)(d1 + X)], Memory[(byte)(d1 + X + 1)]], 0)] = value;
                    break;
                case AddressingMode.IndirectY:
                    Memory[(ushort)(BitConverter.ToUInt16([Memory[d1], Memory[(byte)(d1 + 1)]], 0) + Y)] = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(addressingMode), addressingMode, null);
            }
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
            PC = 0xFFFC;
            SP = SP -= 3;
            Status.InterruptDisable = true;
        }

        private void WriteLog(byte opcode, byte d1, byte d2, Command command)
        {
            var addMode = command.AddressingMode;
            string b1 = addMode == AddressingMode.Implied ? "  " : $"{d1:X2}";
            string b2 = new[] { AddressingMode.Absolute, AddressingMode.AbsoluteX, AddressingMode.AbsoluteY, AddressingMode.Indirect }.Contains(addMode) ? $"{d2:X2}" : "  ";
            string logLine = $"{PC:X4}  {opcode:X2} {b1} {b2}  {command.GetType().Name} {ParseAddress(d1, d2, addMode),-26}  A:{Acc:X2} X:{X:X2} Y:{Y:X2} P:{Status.GetStatus():X2} SP:{SP:X2} CYC:{_cycles}";
            _logWriter!.WriteLine(logLine);
        }

        private string ParseAddress(byte d1, byte d2, AddressingMode addressingMode)
        {
            return addressingMode switch
            {
                AddressingMode.Implied => "",
                AddressingMode.Accumulator => "",
                AddressingMode.Immediate => $"#${d1:X2}",
                AddressingMode.ZeroPage => $"${d1:X2} = {Memory[d1]:X2}",
                AddressingMode.ZeroPageX => $"${d1:X2},X",
                AddressingMode.ZeroPageY => $"${d1:X2},Y",
                AddressingMode.Relative => $"${PC + (sbyte)d1:X2}",
                AddressingMode.Absolute => $"${BitConverter.ToUInt16([d1, d2], 0):X4} = {Memory[BitConverter.ToUInt16([d1, d2], 0)]:X2}",
                AddressingMode.AbsoluteX => $"${BitConverter.ToUInt16([d1, d2], 0):X4},X",
                AddressingMode.AbsoluteY => $"${BitConverter.ToUInt16([d1, d2], 0):X4},Y",
                AddressingMode.Indirect => $"(${BitConverter.ToUInt16([d1, d2], 0):X4}) = {BitConverter.ToUInt16(
                    [Memory[BitConverter.ToUInt16([d1, d2], 0)],
                     (BitConverter.ToUInt16([d1, d2], 0) & 0x00FF) == 0xFF
                        ? Memory[BitConverter.ToUInt16([d1, d2], 0) & 0xFF00]
                        : Memory[BitConverter.ToUInt16([d1, d2], 0) + 1]], 0):X4}",
                AddressingMode.IndirectX => $"(${d1:X2},X)",
                AddressingMode.IndirectY => $"(${d1:X2}),Y = {BitConverter.ToUInt16([Memory[d1], Memory[(byte)(d1 + 1)]], 0):X4} @ {(ushort)(BitConverter.ToUInt16([Memory[d1], Memory[(byte)(d1 + 1)]], 0) + Y):X4} = {Memory[(ushort)(BitConverter.ToUInt16([Memory[d1], Memory[(byte)(d1 + 1)]], 0) + Y)]:X2}",
                _ => throw new ArgumentOutOfRangeException(nameof(addressingMode), addressingMode, null),
            };
        }
    }
}
