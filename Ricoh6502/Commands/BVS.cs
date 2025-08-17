namespace Ricoh6502.Commands
{
    public class BVS : BranchCommand
    {
        public BVS(byte d1) : base(d1) { }

        protected override bool CheckCondition(Status status)
        {
            return status.OverflowFlag;
        }
    }
}