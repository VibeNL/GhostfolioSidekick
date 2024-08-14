using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class RepayBondActivity : Activity
	{
		internal RepayBondActivity() : base()
		{
			// EF Core
			TotalRepayAmount = null!;
		}

		public RepayBondActivity(
			Account account,
			DateTime dateTime,
			Money totalRepayAmount,
			string? transactionId,
			int? sortingPriority,
			string? description) : base(account, dateTime, transactionId, sortingPriority, description)
		{
			TotalRepayAmount = totalRepayAmount;
		}

		public Money TotalRepayAmount { get; }

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{Date}";
		}

		protected override Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, Activity otherActivity)
		{
			return Task.FromResult(true);
		}
	}
}
