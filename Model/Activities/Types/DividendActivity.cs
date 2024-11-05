﻿using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class DividendActivity : Activity, IActivityWithPartialIdentifier
	{
		public DividendActivity()
		{
			// EF Core
			Amount = null!;
		}

		public DividendActivity(
			Account account,
			Holding? holding,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			Money amount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, dateTime, transactionId, sortingPriority, description)
		{
			PartialSymbolIdentifiers = [.. partialSymbolIdentifiers];
			Amount = amount;
		}

		public ICollection<Money> Fees { get; set; } = [];

		public virtual IList<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; } = [];

		public Money Amount { get; set; }

		public ICollection<Money> Taxes { get; set; } = [];

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
