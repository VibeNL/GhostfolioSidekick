﻿using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class DividendActivity : Activity, IActivityWithPartialIdentifier
	{
		public DividendActivity()
		{
			// EF Core
			Amount = null!;
			SymbolProfile = null!;
		}

		public DividendActivity(
			SymbolProfile? symbolProfile,
			Account account,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			Money amount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, dateTime, transactionId, sortingPriority, description)
		{
			PartialSymbolIdentifiers = [.. partialSymbolIdentifiers];
			SymbolProfile = symbolProfile;
			Amount = amount;
		}

		public ICollection<Money> Fees { get; set; } = [];

		public virtual IList<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; } = [];

		public SymbolProfile? SymbolProfile { get; }
		
		public Money Amount { get; set; }

		public ICollection<Money> Taxes { get; set; } = [];

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
