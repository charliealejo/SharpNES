namespace Ricoh6502.Commands
{
    public class BCC : BranchCommand
    {
        public BCC(byte d1) : base(d1) { }

        protected override bool CheckCondition(Status status)
        {
            return !status.CarryFlag;
        }
    }
}