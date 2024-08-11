﻿using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class GiftActivity : ActivityWithQuantityAndUnitPrice
	{
		public GiftActivity(
		Account account,
		DateTime dateTime,
		decimal amount,
		string? transactionId) : base(account, dateTime, amount, null, transactionId, null, null)
		{
		}

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{Date}";
		}

		protected override Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, Activity otherActivity)
		{
			var otherGiftActivity = (GiftActivity)otherActivity;
			var quantityTimesUnitPriceEquals = CompareUtilities.AreNumbersEquals(
				Quantity,
				otherGiftActivity.Quantity);
			return Task.FromResult(quantityTimesUnitPriceEquals);
		}
	}
}
