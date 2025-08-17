namespace Ricoh6502.Commands
{
    public class BMI : BranchCommand
    {
        public BMI(byte d1) : base(d1) { }

        protected override bool CheckCondition(Status status)
        {
            return status.NegativeFlag;
        }
    }
}