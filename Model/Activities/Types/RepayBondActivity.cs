using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class RepayBondActivity : BaseActivity<RepayBondActivity>
	{
		public RepayBondActivity(
		Account account,
		DateTime dateTime,
		Money totalRepayAmount,
		string? transactionId)
		{
			Account = account;
			Date = dateTime;
			TotalRepayAmount = totalRepayAmount;
			TransactionId = transactionId;
		}

		public override Account Account { get; }

		public override DateTime Date { get; }

		public Money TotalRepayAmount { get; }

		public override string? TransactionId { get; set; }

		public override int? SortingPriority { get; set; }

		public override string? Id { get; set; }

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{Date}";
		}

		protected override Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, RepayBondActivity otherActivity)
		{
			return Task.FromResult(true);
		}
	}
}
