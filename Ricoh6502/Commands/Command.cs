namespace Ricoh6502.Commands
{
    public abstract class Command(AddressingMode addressingMode)
    {
        public AddressingMode AddressingMode { get; protected set; } = addressingMode;
        protected byte D1 { get; set; }
        protected byte D2 { get; set; }
        protected bool PageCrossed { get; set; } = false;

        /// <summary>
        /// Executes the command using the specified processor.
        /// </summary>
        /// <param name="processor">Instance of the processor</param>
        /// <returns>The number of cycles taken to execute the command</returns>
        public virtual byte Execute(Processor processor)
        {
            ExecuteInternal(processor);
            var nextInstructionAddress = GetNextInstructionAddress(processor);
            var cycles = GetInstructionCycleCount();
            if (PageCrossed)
            {
                cycles += 1;
            }
            processor.PC = nextInstructionAddress;
            return cycles;
        }

        /// <summary>
        /// Executes the command's internal logic.
        /// </summary>
        /// <param name="processor">Instance of the processor</param>
        protected abstract void ExecuteInternal(Processor processor);

        /// <summary>
        /// Gets the number of CPU cycles required to execute the instruction.
        /// </summary>
        /// <returns>The number of CPU cycles.</returns>
        protected abstract byte GetInstructionCycleCount();

        /// <summary>
        /// Gets the address of the next instruction to be executed.
        /// </summary>
        /// <returns>The address of the next instruction.</returns>
        protected abstract ushort GetNextInstructionAddress(Processor processor);

        /// <summary>
        /// Checks if the instruction crosses a page boundary.
        /// </summary>
        /// <param name="baseAddress">Base address (provided in the instruction).</param>
        /// <param name="effectiveAddress">Effective address (calculated during execution).</param>
        /// <returns>true if the instruction crosses a page boundary; otherwise, false.</returns>
        protected abstract bool CheckForPageBoundaryCrossing(ushort baseAddress, ushort effectiveAddress);
    }
}