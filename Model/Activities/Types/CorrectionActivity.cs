using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
    public record class CorrectionActivity : ActivityWithAmount
    {
        public CorrectionActivity()
        {
            // EF Core
            Amount = null!;
        }

        public CorrectionActivity(
            Account account,
            Holding? holding,
            DateTime dateTime,
            Money amount,
            string transactionId,
            int? sortingPriority,
            string? description) : base(account, holding, dateTime, amount, transactionId, sortingPriority, description)
        {
        }
    }
}
