namespace Ricoh6502
{
    public class MemoryAccessEventArgs : EventArgs
    {
        public int Register { get; }
        public byte Value { get; }

        public MemoryAccessEventArgs(int register, byte value)
        {
            Register = register;
            Value = value;
        }
    }
}