using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Ghostfolio.API.Mapper
{
	public class ModelToContractMapper(GhostfolioAPI.API.IExchangeRateService exchangeRateService)
	{
		public GhostfolioAPI.Contract.Activity? ConvertToGhostfolioActivity(Model.Accounts.Account account, Model.Activities.Activity activity)
		{
			decimal CalculateFee(IEnumerable<Money> fees, Currency targetCurrency, DateTime dateTime)
			{
				decimal amount = 0;

				foreach (var fee in fees)
				{
					var rate = exchangeRateService.GetConversionRate(fee.Currency, targetCurrency, dateTime);

					amount += rate * fee.Amount;
				}

				return amount;
			}

			if (activity.ActivityType == Model.Activities.ActivityType.Interest || activity.ActivityType == Model.Activities.ActivityType.Fee)
			{
				return new GhostfolioAPI.Contract.Activity
				{
					AccountId = account.Id,
					Currency = account.Balance.Money.Currency.Symbol,
					SymbolProfile = null,
					Comment = activity.Comment,
					Date = activity.Date,
					Fee = CalculateFee(activity.Fees, account.Balance.Currency),
					FeeCurrency = account.Balance.Currency.Symbol,
					Quantity = activity.Quantity,
					Type = ParseType(activity.ActivityType),
					UnitPrice = currentPriceCalculator.GetConvertedPrice(activity.UnitPrice, account.Balance.Currency, activity.Date)!.Amount,
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
					AssetClass = activity.Asset.AssetClass.ToString(),
					AssetSubClass = activity.Asset.AssetSubClass?.ToString(),
					Currency = activity.Asset.Currency!.Symbol,
					DataSource = activity.Asset.DataSource!,
					Name = activity.Asset.Name
				},
				Comment = activity.Comment,
				Date = activity.Date,
				Fee = CalculateFee(activity.Fees, activity.Asset.Currency),
				FeeCurrency = activity.Asset.Currency?.Symbol,
				Quantity = activity.Quantity,
				Type = ParseType(activity.ActivityType),
				UnitPrice = currentPriceCalculator.GetConvertedPrice(activity.UnitPrice, activity.Asset.Currency!, activity.Date)?.Amount ?? 0,
				ReferenceCode = activity.ReferenceCode
			};
		}

		private ActivityType ParseType(ActivityType? type)
		{
			switch (type)
			{
				case null:
					return ActivityType.IGNORE;
				case ActivityType.Buy:
					return ActivityType.BUY;
				case ActivityType.Sell:
					return ActivityType.SELL;
				case ActivityType.Dividend:
					return ActivityType.DIVIDEND;
				case ActivityType.Send:
					return ActivityType.SELL; // TODO: 
				case ActivityType.Receive:
					return ActivityType.BUY; // TODO: 
				case ActivityType.Convert:
					return ActivityType.IGNORE; // TODO: 
				case ActivityType.Interest:
					return ActivityType.INTEREST;
				case ActivityType.Fee:
					return ActivityType.FEE;
				case ActivityType.Gift:
					return ActivityType.BUY; // TODO: 
				case ActivityType.LearningReward:
					return ActivityType.IGNORE; // TODO: 
				case ActivityType.StakingReward:
					return ActivityType.IGNORE; // TODO: 
				default:
					throw new NotSupportedException($"ActivityType {type} not supported");
			}
		}
	}
}
