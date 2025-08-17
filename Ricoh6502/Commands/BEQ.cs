namespace Ricoh6502.Commands
{
    public class BEQ : BranchCommand
    {
        public BEQ(byte d1) : base(d1) { }

        protected override bool CheckCondition(Status status)
        {
            return status.ZeroFlag;
        }
    }
}