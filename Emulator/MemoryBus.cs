using System.Runtime.CompilerServices;
using InputDevices;
using Ricoh6502;

namespace NESPPU
{
    public class MemoryBus
    {
        private readonly CPU _cpu;
        private readonly PPU _ppu;

        public MemoryBus(CPU cpu, PPU ppu)
        {
            _cpu = cpu ?? throw new ArgumentNullException(nameof(cpu));
            _ppu = ppu ?? throw new ArgumentNullException(nameof(ppu));
        }

        public void Initialize()
        {
            _cpu.PPURegisterAccessed += OnPPURegisterAccessed;
            _cpu.PPURegisterRead += (sender, args) => args.Value = _ppu.Registers.HandleRegisterRead(args.Register);
            _cpu.DMAWrite += OnDMAWrite;
            _ppu.Registers.PPUStatusChanged += (s, e) =>
            {
                for (ushort addr = 0x2002; addr <= 0x3FFF; addr += 8)
                {
                    _cpu.Memory[addr] = e;
                }
            };
            _ppu.TriggerNMI += (s, e) =>
            {
                _cpu.NMI();
            };
        }

        private void OnPPURegisterAccessed(object? sender, Ricoh6502.MemoryAccessEventArgs e)
        {
            _ppu.Registers.OpenBus = e.Value;
            switch (e.Register)
            {
                case 0:
                    _ppu.Registers.PPUCTRL = e.Value;
                    break;
                case 1:
                    _ppu.Registers.PPUMASK = e.Value;
                    break;
                case 3:
                    _ppu.Registers.OAMADDR = e.Value;
                    break;
                case 4:
                    _ppu.Registers.OAMDATA = e.Value;
                    break;
                case 5:
                    _ppu.Registers.PPUSCROLL = e.Value;
                    break;
                case 6:
                    _ppu.Registers.PPUADDR = e.Value;
                    break;
                case 7:
                    _ppu.Registers.PPUDATA = e.Value;
                    break;
                case 0x14:
                    _ppu.Registers.OAMDMA = e.Value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(e.Register), "Invalid PPU register");
            }
        }

        private void OnDMAWrite(object? sender, Ricoh6502.MemoryAccessEventArgs e) 
        {
            // DMA writes should start at the current OAMADDR and wrap around
            uint targetAddress = (_ppu.Registers.OAMADDR + e.Register) & 0xFF;
            _ppu.OAM[targetAddress] = e.Value;
        }
    }
}