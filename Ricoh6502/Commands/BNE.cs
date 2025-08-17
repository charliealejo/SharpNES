namespace Ricoh6502.Commands
{
    public class BNE : BranchCommand
    {
        public BNE(byte d1) : base(d1) { }

        protected override bool CheckCondition(Status status)
        {
            return !status.ZeroFlag;
        }
    }
}