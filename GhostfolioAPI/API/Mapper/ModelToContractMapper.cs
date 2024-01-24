using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Ghostfolio.API.Mapper
{
	public static class ModelToContractMapper
	{
		public static async Task<GhostfolioAPI.Contract.Activity?> ConvertToGhostfolioActivity(
			IExchangeRateService exchangeRateService,
			Model.Symbols.SymbolProfile? symbolProfile,
			Model.Activities.Activity activity)
		{
			async Task<decimal> CalculateFee(IEnumerable<Money> fees, Currency targetCurrency, DateTime dateTime)
			{
				decimal amount = 0;

				foreach (var fee in fees)
				{
					amount += await ConvertPrice(exchangeRateService, fee, targetCurrency, dateTime);
				}

				return amount;
			}

			if (activity.ActivityType == Model.Activities.ActivityType.Interest ||
				activity.ActivityType == Model.Activities.ActivityType.Fee)
			{
				return new GhostfolioAPI.Contract.Activity
				{
					AccountId = activity.Account.Id,
					Currency = activity.Account.Balance.Money.Currency.Symbol,
					SymbolProfile = null,
					Comment = TransactionReferenceUtilities.GetComment(activity),
					Date = activity.Date,
					Fee = await CalculateFee(activity.Fees, activity.Account.Balance.Money.Currency, activity.Date),
					FeeCurrency = activity.Account.Balance.Money.Currency.Symbol,
					Quantity = activity.Quantity,
					Type = ParseType(activity.ActivityType),
					UnitPrice = await ConvertPrice(exchangeRateService, activity.UnitPrice, activity.Account.Balance.Money.Currency, activity.Date),
					ReferenceCode = activity.TransactionId
				};
			}

			if (symbolProfile == null)
			{
				// Ignore for now
				return null;
			}

			return new GhostfolioAPI.Contract.Activity
			{
				AccountId = activity.Account.Id,
				Currency = symbolProfile.Currency.Symbol,
				SymbolProfile = new GhostfolioAPI.Contract.SymbolProfile
				{
					Symbol = symbolProfile.Symbol,
					AssetClass = symbolProfile.AssetClass.ToString(),
					AssetSubClass = symbolProfile.AssetSubClass?.ToString(),
					Currency = symbolProfile.Currency!.Symbol,
					DataSource = symbolProfile.DataSource.ToString(),
					Name = symbolProfile.Name
				},
				Comment = TransactionReferenceUtilities.GetComment(activity, symbolProfile),
				Date = activity.Date,
				Fee = await CalculateFee(activity.Fees, symbolProfile.Currency, activity.Date),
				FeeCurrency = symbolProfile.Currency.Symbol,
				Quantity = activity.Quantity,
				Type = ParseType(activity.ActivityType),
				UnitPrice = await ConvertPrice(exchangeRateService, activity.UnitPrice, symbolProfile.Currency, activity.Date),
				ReferenceCode = activity.TransactionId
			};
		}

		private static async Task<decimal> ConvertPrice(IExchangeRateService exchangeRateService, Money money, Currency targetCurrency, DateTime dateTime)
		{
			var rate = await exchangeRateService.GetConversionRate(money.Currency, targetCurrency, dateTime);
			return money.Amount * rate;
		}

		private static GhostfolioAPI.Contract.ActivityType ParseType(Model.Activities.ActivityType? type)
		{
			switch (type)
			{
				case null:
					return GhostfolioAPI.Contract.ActivityType.IGNORE;
				case Model.Activities.ActivityType.Buy:
					return GhostfolioAPI.Contract.ActivityType.BUY;
				case Model.Activities.ActivityType.Sell:
					return GhostfolioAPI.Contract.ActivityType.SELL;
				case Model.Activities.ActivityType.Dividend:
					return GhostfolioAPI.Contract.ActivityType.DIVIDEND;
				case Model.Activities.ActivityType.Send:
					return GhostfolioAPI.Contract.ActivityType.SELL;
				case Model.Activities.ActivityType.Receive:
					return GhostfolioAPI.Contract.ActivityType.BUY;
				case Model.Activities.ActivityType.Convert:
					return GhostfolioAPI.Contract.ActivityType.IGNORE;
				case Model.Activities.ActivityType.Interest:
					return GhostfolioAPI.Contract.ActivityType.INTEREST;
				case Model.Activities.ActivityType.Fee:
					return GhostfolioAPI.Contract.ActivityType.FEE;
				case Model.Activities.ActivityType.Gift:
					return GhostfolioAPI.Contract.ActivityType.BUY;
				case Model.Activities.ActivityType.LearningReward:
					return GhostfolioAPI.Contract.ActivityType.IGNORE;
				case Model.Activities.ActivityType.StakingReward:
					return GhostfolioAPI.Contract.ActivityType.IGNORE;
				default:
					throw new NotSupportedException($"ActivityType {type} not supported");
			}
		}
	}
}
