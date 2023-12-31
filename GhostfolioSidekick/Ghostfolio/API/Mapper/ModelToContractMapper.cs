﻿using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Ghostfolio.API.Mapper
{
	public class ModelToContractMapper
	{
		private readonly ICurrentPriceCalculator currentPriceCalculator;

		public ModelToContractMapper(ICurrentPriceCalculator currentPriceCalculator)
		{
			this.currentPriceCalculator = currentPriceCalculator;
		}

		public Contract.Activity ConvertToGhostfolioActivity(Account account, Activity activity)
		{
			decimal CalculateFee(IEnumerable<Money> fees, Currency targetCurrency)
			{
				decimal amount = 0;

				foreach (var fee in fees)
				{
					amount += currentPriceCalculator.GetConvertedPrice(fee, targetCurrency, fee.TimeOfRecord)?.Amount ?? 0;
				}

				return amount;
			}

			if (activity.ActivityType == ActivityType.Interest || activity.ActivityType == ActivityType.Fee)
			{
				return new Contract.Activity
				{
					AccountId = account.Id,
					Currency = account.Balance.Currency.Symbol,
					SymbolProfile = null,
					Comment = activity.Comment,
					Date = activity.Date,
					Fee = CalculateFee(activity.Fees, account.Balance.Currency),
					FeeCurrency = account.Balance.Currency.Symbol,
					Quantity = activity.Quantity,
					Type = ParseType(activity.ActivityType),
					UnitPrice = currentPriceCalculator.GetConvertedPrice(activity.UnitPrice, account.Balance.Currency, activity.Date).Amount,
					ReferenceCode = activity.ReferenceCode
				};
			}

			if (activity.Asset == null)
			{
				return null;
			}

			return new Contract.Activity
			{
				AccountId = account.Id,
				Currency = activity.Asset.Currency?.Symbol,
				SymbolProfile = new Contract.SymbolProfile
				{
					Symbol = activity.Asset.Symbol,
					AssetClass = activity.Asset.AssetClass?.ToString(),
					AssetSubClass = activity.Asset.AssetSubClass?.ToString(),
					Currency = activity.Asset.Currency.Symbol,
					DataSource = activity.Asset.DataSource,
					Name = activity.Asset.Name
				},
				Comment = activity.Comment,
				Date = activity.Date,
				Fee = CalculateFee(activity.Fees, activity.Asset.Currency),
				FeeCurrency = activity.Asset.Currency?.Symbol,
				Quantity = activity.Quantity,
				Type = ParseType(activity.ActivityType),
				UnitPrice = currentPriceCalculator.GetConvertedPrice(activity.UnitPrice, activity.Asset.Currency, activity.Date)?.Amount ?? 0,
				ReferenceCode = activity.ReferenceCode
			};
		}

		private Contract.ActivityType ParseType(ActivityType? type)
		{
			switch (type)
			{
				case null:
					return Contract.ActivityType.IGNORE;
				case ActivityType.Buy:
					return Contract.ActivityType.BUY;
				case ActivityType.Sell:
					return Contract.ActivityType.SELL;
				case ActivityType.Dividend:
					return Contract.ActivityType.DIVIDEND;
				case ActivityType.Send:
					return Contract.ActivityType.SELL; // TODO: 
				case ActivityType.Receive:
					return Contract.ActivityType.BUY; // TODO: 
				case ActivityType.Convert:
					return Contract.ActivityType.IGNORE; // TODO: 
				case ActivityType.Interest:
					return Contract.ActivityType.INTEREST;
				case ActivityType.Fee:
					return Contract.ActivityType.FEE;
				case ActivityType.Gift:
					return Contract.ActivityType.BUY; // TODO: 
				case ActivityType.LearningReward:
					return Contract.ActivityType.IGNORE; // TODO: 
				case ActivityType.StakingReward:
					return Contract.ActivityType.IGNORE; // TODO: 
				default:
					throw new NotSupportedException($"ActivityType {type} not supported");
			}
		}
	}
}
