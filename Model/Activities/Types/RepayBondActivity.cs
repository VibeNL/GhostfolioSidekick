﻿using GhostfolioSidekick.Model.Accounts;

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
			Account account,
			Holding? holding,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime dateTime,
			Money totalRepayAmount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, dateTime, transactionId, sortingPriority, description)
		{
			PartialSymbolIdentifiers = [.. partialSymbolIdentifiers];
			TotalRepayAmount = totalRepayAmount;
		}

		public virtual List<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; } = [];

		public Money TotalRepayAmount { get; }
	}
}
