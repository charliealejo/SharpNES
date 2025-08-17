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
        public bool CarryFlag { get; set; }

        /// <summary>
        /// Zero Flag
        /// </summary>
        /// <remarks>
        /// The zero flag is set if the result of the last operation as was zero.
        /// </remarks>
        public bool ZeroFlag { get; set; }

        /// <summary>
        /// Interrupt Disable
        /// </summary>
        /// <remarks>
        /// The interrupt disable flag is set if the program has executed a 'Set Interrupt Disable' (SEI)
        /// instruction. While this flag is set the processor will not respond to interrupts from devices
        /// until it is cleared by a 'Clear Interrupt Disable' (CLI) instruction.
        /// </remarks>
        public bool InterruptDisable { get; set; }

        /// <summary>
        /// Decimal Mode
        /// </summary>
        /// <remarks>
        /// While the decimal mode flag is set the processor will obey the rules of Binary Coded Decimal (BCD)
        /// arithmetic during addition and subtraction. The flag can be explicitly set using 'Set Decimal Flag'
        /// (SED) and cleared with 'Clear Decimal Flag' (CLD).
        /// </remarks>
        public bool DecimalMode { get; set; }

        /// <summary>
        /// Break Command
        /// </summary>
        /// <remarks>
        /// The break command bit is set when a BRK instruction has been executed and an interrupt has been
        /// generated to process it.
        /// </remarks>
        public bool BreakCommand { get; set; }

        /// <summary>
        /// Overflow Flag
        /// </summary>
        /// <remarks>
        /// The overflow flag is set during arithmetic operations if the result has yielded an invalid 2's
        /// complement result (e.g. adding to positive numbers and ending up with a negative result:
        /// 64 + 64 => -128). It is determined by looking at the carry between bits 6 and 7 and between bit 7
        /// and the carry flag.
        /// </remarks>
        public bool OverflowFlag { get; set; }

        /// <summary>
        /// Negative Flag
        /// </summary>
        /// <remarks>
        /// The negative flag is set if the result of the last operation had bit 7 set to a one.
        /// </remarks>
        public bool NegativeFlag { get; set; }

        public void SetZeroAndNegativeFlags(byte value)
        {
            ZeroFlag = value == 0;
            NegativeFlag = (value & 0x80) != 0;
        }
    }
}
