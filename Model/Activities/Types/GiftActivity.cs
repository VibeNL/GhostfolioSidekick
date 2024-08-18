﻿using GhostfolioSidekick.Model.Accounts;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class GiftActivity : ActivityWithQuantityAndUnitPrice
	{
		internal GiftActivity()
		{			
		}

		public GiftActivity(
		Account account,
		DateTime dateTime,
		decimal amount,
		string? transactionId) : base(account, dateTime, amount, null, transactionId, null, null)
		{
		}

		public override string ToString()
		{
			return $"{Account}_{Date}";
		}
	}
}
