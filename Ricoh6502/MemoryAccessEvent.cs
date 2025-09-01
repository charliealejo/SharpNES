namespace Ricoh6502
{
    public class MemoryAccessEventArgs : EventArgs
    {
        public uint Register { get; }
        public byte Value { get; set; }

        public MemoryAccessEventArgs(uint register, byte value)
        {
            Register = register;
            Value = value;
        }
    }
}