namespace Ricoh6502
{
    /// <summary>
    /// Status Register
    /// </summary>
    /// <remarks>
    /// The status register contains flags that provide information about the state of the processor.
    /// It contains the following flags:
    /// <list type="bullet">
    /// <item>Carry Flag</item>
    /// <item>Zero Flag</item>
    /// <item>Interrupt Disable</item>
    /// <item>Decimal Mode</item>
    /// <item>Break Command</item>
    /// <item>Overflow Flag</item>
    /// <item>Negative Flag</item>
    /// </list>
    /// </remarks>
    public class Status
    {
        /// <summary>
        /// Carry Flag
        /// </summary>
        /// <remarks>
        /// The carry flag is set if the last operation caused an overflow from bit 7 of the result or an
        /// underflow from bit 0. This condition is set during arithmetic, comparison and during logical shifts.
        /// It can be explicitly set using the 'Set Carry Flag' (SEC) instruction and cleared with
        /// 'Clear Carry Flag' (CLC).
        /// </remarks>
        public bool CarryFlag { get; set; } = false;

        /// <summary>
        /// Zero Flag
        /// </summary>
        /// <remarks>
        /// The zero flag is set if the result of the last operation as was zero.
        /// </remarks>
        public bool ZeroFlag { get; set; } = false;

        /// <summary>
        /// Interrupt Disable
        /// </summary>
        /// <remarks>
        /// The interrupt disable flag is set if the program has executed a 'Set Interrupt Disable' (SEI)
        /// instruction. While this flag is set the processor will not respond to interrupts from devices
        /// until it is cleared by a 'Clear Interrupt Disable' (CLI) instruction.
        /// </remarks>
        public bool InterruptDisable { get; set; } = true;

        /// <summary>
        /// Decimal Mode
        /// </summary>
        /// <remarks>
        /// While the decimal mode flag is set the processor will obey the rules of Binary Coded Decimal (BCD)
        /// arithmetic during addition and subtraction. The flag can be explicitly set using 'Set Decimal Flag'
        /// (SED) and cleared with 'Clear Decimal Flag' (CLD).
        /// </remarks>
        public bool DecimalMode { get; set; } = false;

        /// <summary>
        /// Break Command
        /// </summary>
        /// <remarks>
        /// The break command bit is set when a BRK instruction has been executed and an interrupt has been
        /// generated to process it.
        /// </remarks>
        public bool BreakCommand { get; set; } = false;

        /// <summary>
        /// Overflow Flag
        /// </summary>
        /// <remarks>
        /// The overflow flag is set during arithmetic operations if the result has yielded an invalid 2's
        /// complement result (e.g. adding to positive numbers and ending up with a negative result:
        /// 64 + 64 => -128). It is determined by looking at the carry between bits 6 and 7 and between bit 7
        /// and the carry flag.
        /// </remarks>
        public bool OverflowFlag { get; set; } = false;

        /// <summary>
        /// Negative Flag
        /// </summary>
        /// <remarks>
        /// The negative flag is set if the result of the last operation had bit 7 set to a one.
        /// </remarks>
        public bool NegativeFlag { get; set; } = false;

        /// <summary>
        /// Sets the Zero and Negative flags based on the given value.
        /// </summary>
        /// <remarks>
        /// Sets the zero flag when the value is zero.
        /// Sets the negative flag when the value is negative (the most significant bit is set).
        /// </remarks>
        /// <param name="value">The value to check.</param>
        public void SetZeroAndNegativeFlags(byte value)
        {
            ZeroFlag = value == 0;
            NegativeFlag = (value & 0x80) != 0;
        }

        /// <summary>
        /// Sets the Carry and Overflow flags based on the given values.
        /// </summary>
        /// <remarks>
        /// Sets the carry flag when the result is greater than 0xFF.
        /// Sets the overflow flag based on the given accumulator and memory values.
        /// </remarks>
        /// <param name="result">The result of the current operation.</param>
        /// <param name="acc">The accumulator value before the operation.</param>
        /// <param name="memory">The memory value.</param>
        public void SetCarryAndOverflowFlags(int result, byte acc, byte memory)
        {
            CarryFlag = result > 0xFF || result < 0;
            OverflowFlag = ((acc ^ (byte)result) & (acc ^ memory) & 0x80) != 0;
        }

        public byte GetStatus()
        {
            byte status = 0;
            status |= (byte)(CarryFlag ? 1 : 0);
            status |= (byte)(ZeroFlag ? 1 << 1 : 0);
            status |= (byte)(InterruptDisable ? 1 << 2 : 0);
            status |= (byte)(DecimalMode ? 1 << 3 : 0);
            status |= (byte)((BreakCommand ? 1 : 0) << 4);
            status |= (byte)((BreakCommand ? 1 : 0) << 5);
            status |= (byte)(OverflowFlag ? 1 << 6 : 0);
            status |= (byte)(NegativeFlag ? 1 << 7 : 0);
            return status;
        }

        public void SetStatus(byte status)
        {
            CarryFlag = (status & 1) != 0;
            ZeroFlag = (status & 2) != 0;
            InterruptDisable = (status & 4) != 0;
            DecimalMode = (status & 8) != 0;
            BreakCommand = false;
            OverflowFlag = (status & 64) != 0;
            NegativeFlag = (status & 128) != 0;
        }
    }
}
