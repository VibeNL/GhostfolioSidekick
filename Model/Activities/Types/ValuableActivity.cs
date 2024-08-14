﻿using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public record class ValuableActivity : Activity
	{
		internal ValuableActivity() : base()
		{
			// EF Core
			Price = null!;
		}

		public ValuableActivity(
			Account account,
			DateTime dateTime,
			Money amount,
			string? transactionId,
			int? sortingPriority,
			string? description) : base(account, dateTime, transactionId, sortingPriority, description)
		{
			Price = amount;
		}

		public Money Price { get; set; }

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{Date}";
		}

		protected override async Task<bool> AreEqualInternal(IExchangeRateService exchangeRateService, Activity otherActivity)
		{
			var otherValuableActivity = (ValuableActivity)otherActivity;
			var existingAmount = await CompareUtilities.Convert(exchangeRateService, otherValuableActivity.Price, Price.Currency, Date);
			var quantityTimesUnitPriceEquals = CompareUtilities.AreNumbersEquals(
				Price,
				existingAmount);
			var feesAndTaxesEquals = CompareUtilities.AreMoneyEquals(
				exchangeRateService,
				otherValuableActivity.Price.Currency,
				otherValuableActivity.Date,
				[], []);
			return quantityTimesUnitPriceEquals &&
				feesAndTaxesEquals;
		}
	}
}
