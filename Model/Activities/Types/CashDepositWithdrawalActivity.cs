﻿using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record CashDepositWithdrawalActivity : Activity
	{
		public CashDepositWithdrawalActivity()
		{
			// EF Core
			Amount = null!;
		}

		public CashDepositWithdrawalActivity(
			Account account,
			Holding? holding,
			DateTime dateTime,
			Money amount,
			string transactionId,
			int? sortingPriority,
			string? description) : base(account, holding, dateTime, transactionId, sortingPriority, description)
		{
			Amount = amount;
		}

		public Money Amount { get; set; }
	}
}
