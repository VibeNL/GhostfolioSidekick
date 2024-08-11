﻿using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record SendAndReceiveActivity : ActivityWithQuantityAndUnitPrice
	{
		public SendAndReceiveActivity(
		Account account,
		DateTime dateTime,
		decimal amount,
		string? transactionId) : base(account, dateTime, amount, null, transactionId, null, null)
		{
		}

		public IEnumerable<Money> Fees { get; set; } = [];

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{Date}";
		}

		protected override Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, Activity otherActivity)
		{
			var otherSendAndReceiveActivity = (SendAndReceiveActivity)otherActivity;
			var quantityTimesUnitPriceEquals = CompareUtilities.AreNumbersEquals(
				Quantity,
				otherSendAndReceiveActivity.Quantity);

			var feesEquals = CompareUtilities.AreMoneyEquals(
				exchangeRateService,
				otherSendAndReceiveActivity.UnitPrice?.Currency ?? Currency.USD,
				otherSendAndReceiveActivity.Date,
				Fees.ToList(),
				otherSendAndReceiveActivity.Fees.ToList());
			return Task.FromResult(quantityTimesUnitPriceEquals && feesEquals);
		}
	}
}
