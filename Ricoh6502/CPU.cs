using InputDevices;
using Ricoh6502.Commands;
using System.Runtime.CompilerServices;

namespace Ricoh6502
{
    public class CPU
    {
        private readonly NesController _nesController;
        private readonly CommandFactory _commandFactory;

        private bool _interrupt;
        private bool _nonMaskableInterrupt;
        private bool _executeDMA;
        private uint _dmaCycle;
        private uint _lastDmaCycle;
        private uint _nextInstructionCycle;

        public uint Cycles { get; private set; }

        public event EventHandler<MemoryAccessEventArgs>? PPURegisterWrite;
        public event EventHandler<MemoryAccessEventArgs>? PPURegisterRead;
        public event EventHandler<MemoryAccessEventArgs>? DMAWrite;
        public event EventHandler<MemoryAccessEventArgs>? APURegisterWrite;
        public event EventHandler<MemoryAccessEventArgs>? APURegisterRead;

        private delegate byte ReadHandler(ushort address);
        private delegate void WriteHandler(ushort address, byte value);

        private readonly ReadHandler[] _readHandlers = new ReadHandler[8];   // 8 páginas de 8KB
        private readonly WriteHandler[] _writeHandlers = new WriteHandler[8];

        private readonly ushort[] _absoluteAddressCache = new ushort[0x10000]; // d1,d2 -> address

        public CPU(NesController nesController)
        {
            _nesController = nesController ?? throw new ArgumentNullException(nameof(nesController));
            _commandFactory = new CommandFactory();
            _interrupt = false;
            _nonMaskableInterrupt = false;
            _executeDMA = false;
            _dmaCycle = 0;
            _lastDmaCycle = 0;

            InitializeMemoryHandlers();
            BuildAbsoluteAddressCache();
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
        public void Clock()
        {
            if (!IsCycleExecutingCommand())
            {
                Cycles++;
                return;
            }

            if (_executeDMA)
            {
                // Write 0 to OAMADDR ($2003) before starting DMA
                SetValue(AddressingMode.Absolute, 0x03, 0x20, 0);
                PerformDMA();
                return;
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
                command = _commandFactory.CreateCommand(opcode, d1, d2);
            }

            // Execute the instruction
            _nextInstructionCycle += command.Execute(this);

            // Clear the interrupt flags
            if (command is IRQ)
            {
                _interrupt = false;
            }
            else if (command is NMI)
            {
                _nonMaskableInterrupt = false;
            }

            Cycles++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsCycleExecutingCommand()
        {
            return Cycles == _nextInstructionCycle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IRQ()
        {
            _interrupt = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NMI()
        {
            _nonMaskableInterrupt = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                AddressingMode.Relative => (ushort)(PC + (sbyte)d1),
                AddressingMode.Absolute => BitConverter.ToUInt16([d1, d2], 0),
                AddressingMode.AbsoluteX => (ushort)(BitConverter.ToUInt16([d1, d2], 0) + X),
                AddressingMode.AbsoluteY => (ushort)(BitConverter.ToUInt16([d1, d2], 0) + Y),
                AddressingMode.Indirect => GetIndirectMemoryAddress(d1, d2),
                AddressingMode.IndirectX => BitConverter.ToUInt16([Memory[(byte)(d1 + X)], Memory[(byte)(d1 + X + 1)]], 0),
                AddressingMode.IndirectY => (ushort)(BitConverter.ToUInt16([Memory[d1], Memory[(byte)(d1 + 1)]], 0) + Y),
                _ => throw new ArgumentOutOfRangeException(nameof(addressingMode), addressingMode, null),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            // Dispatch directo por rango - no más condicionales!
            int handlerIndex = memoryAddress >> 13; // Divide por 8192 para obtener el handler correcto
            return _readHandlers[handlerIndex](memoryAddress);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(AddressingMode addressingMode, byte d1, byte d2, byte value)
        {
            if (addressingMode == AddressingMode.Accumulator)
            {
                Acc = value;
                return;
            }

            var memoryAddress = GetEffectiveAddress(addressingMode, d1, d2);

            // Dispatch directo por rango - no más condicionales!
            int handlerIndex = memoryAddress >> 13; // Divide por 8192 para obtener el handler correcto
            _writeHandlers[handlerIndex](memoryAddress, value);
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
            SP = 0xFD;
            Status.InterruptDisable = true;
            Cycles = 0;
            _nextInstructionCycle = 7;
            _executeDMA = false;
            _dmaCycle = 0;
            _lastDmaCycle = 0;
        }

        private void InitializeMemoryHandlers()
        {
            // $0000-$1FFF: RAM (with mirroring)
            _readHandlers[0] = ReadRAM;      // $0000-$1FFF
            _writeHandlers[0] = WriteRAM;

            // $2000-$3FFF: PPU Registers
            _readHandlers[1] = ReadPPU;      // $2000-$3FFF  
            _writeHandlers[1] = WritePPU;

            // $4000-$5FFF: APU + I/O
            _readHandlers[2] = ReadIO;       // $4000-$5FFF
            _writeHandlers[2] = WriteIO;

            // $6000-$7FFF: Battery backed Save RAM
            _readHandlers[3] = ReadSaveRAM;  // $6000-$7FFF
            _writeHandlers[3] = WriteSaveRAM;

            // $8000-$FFFF: PRG ROM (32KB)
            for (int i = 4; i < 8; i++)
            {
                _readHandlers[i] = ReadROM;   // $8000-$FFFF
                _writeHandlers[i] = WriteROM;
            }
        }

        private void BuildAbsoluteAddressCache()
        {
            for (int d1 = 0; d1 <= 0xFF; d1++)
            {
                for (int d2 = 0; d2 <= 0xFF; d2++)
                {
                    _absoluteAddressCache[(d2 << 8) | d1] = (ushort)(d1 | (d2 << 8));
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte ReadRAM(ushort address)
        {
            // Mirror RAM addresses $0000-$07FF to $0800-$1FFF
            return Memory[address & 0x07FF];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteRAM(ushort address, byte value)
        {
            Memory[address & 0x07FF] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte ReadPPU(ushort address)
        {
            var ppuRegister = address % 8;
            var args = new MemoryAccessEventArgs((uint)ppuRegister, 0);
            PPURegisterRead?.Invoke(this, args);
            return args.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WritePPU(ushort address, byte value)
        {
            if (address == 0x4014)
            {
                PPURegisterWrite?.Invoke(this, new MemoryAccessEventArgs(0x14, value));
                _executeDMA = true;
            }
            else
            {
                var offset = (uint)(address % 8);
                PPURegisterWrite?.Invoke(this, new MemoryAccessEventArgs(offset, value));
                // Actualizar todas las mirrors de una vez
                for (ushort addr = 0x2000; addr < 0x4000; addr += 8)
                {
                    Memory[addr + offset] = value;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte ReadIO(ushort address)
        {
            if (address == 0x4016 || address == 0x4017)
            {
                var buttonState = _nesController.ReadButtonState();
                Memory[address] = buttonState;
                return buttonState;
            }
            else if (address == 0x4015 || address == 0x4017)
            {
                var apuRegister = (uint)(address - 0x4000);
                var args = new MemoryAccessEventArgs(apuRegister, 0);
                APURegisterRead?.Invoke(this, args);
                return args.Value;
            }
            return Memory[address];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteIO(ushort address, byte value)
        {
            if (address == 0x4014)
            {
                WritePPU(address, value); // DMA es una función PPU
            }
            else if (address == 0x4016)
            {
                _nesController.WriteStrobe((byte)(value & 0x07));
            }
            else if (address <= 0x4013 || address == 0x4015 || address == 0x4017)
            {
                var apuRegister = (uint)(address - 0x4000);
                APURegisterWrite?.Invoke(this, new MemoryAccessEventArgs(apuRegister, value));
            }
            Memory[address] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte ReadSaveRAM(ushort address) => Memory[address];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteSaveRAM(ushort address, byte value) => Memory[address] = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte ReadROM(ushort address) => Memory[address];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteROM(ushort address, byte value)
        {
            // ROM writes might trigger mapper functionality
            Memory[address] = value;
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
            else
            {
                var page = Memory[0x4014];
                
                // DMA transfers one byte every other cycle
                if (_dmaCycle % 2 == 1) // Only transfer on odd cycles
                {
                    uint oamAddress = (_dmaCycle - 1) / 2;
                    uint sourceAddress = (uint)(page * 0x100 + oamAddress);
                    DMAWrite?.Invoke(this, new MemoryAccessEventArgs(oamAddress, Memory[sourceAddress]));
                }
                
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
