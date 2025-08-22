using System.Diagnostics;
using Cartridge;
using Logger;
using NESPPU;
using Ricoh6502;
using Ricoh6502.Commands;

namespace Emulator
{
    public class SharpNesEmu
    {
        private const double MillisecondsPerFrame = 16.64;

        private readonly bool _debugMode;
        private readonly ushort _startAddress;
        private readonly NesLogger? _logger;
        private ulong _frameCount;

        public CPU CPU { get; set; }
        public PPU PPU { get; set; }

        public SharpNesEmu(
            string romPath,
            bool debug = false,
            ushort startAddress = 0)
        {
            _debugMode = debug;
            _logger = debug ? new NesLogger() : null;

            CPU = new CPU();
            PPU = new PPU();
            Loader.LoadCartridge(romPath, CPU.Memory, PPU.Memory);
            _startAddress = startAddress;

            var memoryBus = new MemoryBus(CPU, PPU);
            memoryBus.Initialize();

            _frameCount = 0;
        }

        public void Run()
        {
            CPU.Reset();
            if (_startAddress > 0)
            {
                CPU.PC = _startAddress;
            }
            PPU.Reset();

            var clock = new Stopwatch();
            var executing = true;
            var ppuTicksToProcess = PPU.ScanLines * PPU.Dots;
            while (executing)
            {
                clock.Restart();

                for (int i = 0; i < ppuTicksToProcess; i++)
                {
                    if (_frameCount++ % 3 == 0)
                    {
                        if (_debugMode && CPU.IsCycleExecutingCommand())
                        {
                            WriteLog();
                        }
                        executing = CPU.Clock();
                        if (!executing)
                        {
                            break;
                        }
                    }
                    PPU.Clock();
                }

                Thread.Sleep(MillisecondsPerFrame > clock.Elapsed.TotalMilliseconds
                    ? (int)(MillisecondsPerFrame - clock.Elapsed.TotalMilliseconds)
                    : 0);
            }
        }

        private void WriteLog()
        {
            byte opcode = CPU.Memory[CPU.PC];
            byte d1 = CPU.Memory[(ushort)(CPU.PC + 1)];
            byte d2 = CPU.Memory[(ushort)(CPU.PC + 2)];
            var command = CommandFactory.CreateCommand(opcode, d1, d2);

            var addMode = command.AddressingMode;
            string b1 = addMode == AddressingMode.Implied ? "  " : $"{d1:X2}";
            string b2 = new[] { AddressingMode.Absolute, AddressingMode.AbsoluteX, AddressingMode.AbsoluteY, AddressingMode.Indirect }.Contains(addMode) ? $"{d2:X2}" : "  ";
            string logLine = $"{CPU.PC:X4}  {opcode:X2} {b1} {b2}  {command.GetType().Name} {ParseAddress(d1, d2, addMode),-26}  A:{CPU.Acc:X2} X:{CPU.X:X2} Y:{CPU.Y:X2} P:{CPU.Status.GetStatus():X2} SP:{CPU.SP:X2} PPU:{PPU.ScanLine,3},{PPU.Dot,3} CYC:{CPU.Cycles}";
            _logger!.Log(logLine);
        }

        private string ParseAddress(byte d1, byte d2, AddressingMode addressingMode)
        {
            return addressingMode switch
            {
                AddressingMode.Implied => "",
                AddressingMode.Accumulator => "",
                AddressingMode.Immediate => $"#${d1:X2}",
                AddressingMode.ZeroPage => $"${d1:X2} = {CPU.Memory[d1]:X2}",
                AddressingMode.ZeroPageX => $"${d1:X2},X",
                AddressingMode.ZeroPageY => $"${d1:X2},Y",
                AddressingMode.Relative => $"${CPU.PC + (sbyte)d1:X2}",
                AddressingMode.Absolute => $"${BitConverter.ToUInt16([d1, d2], 0):X4} = {CPU.Memory[BitConverter.ToUInt16([d1, d2], 0)]:X2}",
                AddressingMode.AbsoluteX => $"${BitConverter.ToUInt16([d1, d2], 0):X4},X",
                AddressingMode.AbsoluteY => $"${BitConverter.ToUInt16([d1, d2], 0):X4},Y",
                AddressingMode.Indirect => $"(${BitConverter.ToUInt16([d1, d2], 0):X4}) = {BitConverter.ToUInt16(
                    [CPU.Memory[BitConverter.ToUInt16([d1, d2], 0)],
                     (BitConverter.ToUInt16([d1, d2], 0) & 0x00FF) == 0xFF
                        ? CPU.Memory[BitConverter.ToUInt16([d1, d2], 0) & 0xFF00]
                        : CPU.Memory[BitConverter.ToUInt16([d1, d2], 0) + 1]], 0):X4}",
                AddressingMode.IndirectX => $"(${d1:X2},X)",
                AddressingMode.IndirectY => $"(${d1:X2}),Y = {BitConverter.ToUInt16([CPU.Memory[d1], CPU.Memory[(byte)(d1 + 1)]], 0):X4} @ {(ushort)(BitConverter.ToUInt16([CPU.Memory[d1], CPU.Memory[(byte)(d1 + 1)]], 0) + CPU.Y):X4} = {CPU.Memory[(ushort)(BitConverter.ToUInt16([CPU.Memory[d1], CPU.Memory[(byte)(d1 + 1)]], 0) + CPU.Y)]:X2}",
                _ => throw new ArgumentOutOfRangeException(nameof(addressingMode), addressingMode, null),
            };
        }
    }
}
