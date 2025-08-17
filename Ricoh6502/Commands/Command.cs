namespace Ricoh6502.Commands
{
    public abstract class Command(AddressingMode addressingMode)
    {
        protected AddressingMode AddressingMode { get; set; } = addressingMode;
        protected byte D1 { get; set; }
        protected byte D2 { get; set; }

        /// <summary>
        /// Executes the command using the specified processor.
        /// </summary>
        /// <param name="processor">Instance of the processor</param>
        /// <returns>The number of cycles taken to execute the command</returns>
        public byte Execute(Processor processor)
        {
            ExecuteInternal(processor);            
            var nextInstructionAddress = GetNextInstructionAddress(processor);
            var cycles = GetInstructionCycleCount();
            if (CheckForPageBoundaryCrossing(processor.PC, nextInstructionAddress))
            {
                cycles += 1;
            }
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
        /// <param name="currentInstructionAddress">Current instruction address.</param>
        /// <param name="nextInstructionAddress">Next instruction address.</param>
        /// <returns></returns>
        protected abstract bool CheckForPageBoundaryCrossing(ushort currentInstructionAddress, ushort nextInstructionAddress);
    }
}