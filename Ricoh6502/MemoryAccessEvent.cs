namespace Ricoh6502
{
    public class MemoryAccessEventArgs : EventArgs
    {
        public uint Register { get; }
        public byte Value { get; }

        public MemoryAccessEventArgs(uint register, byte value)
        {
            Register = register;
            Value = value;
        }
    }
}