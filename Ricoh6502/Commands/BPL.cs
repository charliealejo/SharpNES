namespace Ricoh6502.Commands
{
    public class BPL : BranchCommand
    {
        public BPL(byte d1) : base(d1) { }

        protected override bool CheckCondition(Status status)
        {
            return !status.NegativeFlag;
        }
    }
}