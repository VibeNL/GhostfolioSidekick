using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class RepayBondActivity : Activity, IActivityWithPartialIdentifier
	{
		public RepayBondActivity()
		{
			// EF Core
			TotalRepayAmount = null!;
		}

		public RepayBondActivity(
			SymbolProfile? symbolProfile,
			Account account,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			Money totalRepayAmount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, dateTime, transactionId, sortingPriority, description)
		{
			PartialSymbolIdentifiers = [.. partialSymbolIdentifiers];
			SymbolProfile = symbolProfile;
			TotalRepayAmount = totalRepayAmount;
		}

		public virtual IList<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; } = [];

		public SymbolProfile? SymbolProfile { get; }

		public Money TotalRepayAmount { get; }

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
