namespace Ricoh6502.Commands
{
    public class BVC : BranchCommand
    {
        public BVC(byte d1) : base(d1) { }

        protected override bool CheckCondition(Status status)
        {
            return !status.OverflowFlag;
        }
    }
}