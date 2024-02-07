using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Compare;

namespace GhostfolioSidekick.GhostfolioAPI.API.Mapper
{
	public static class ModelToContractMapper
	{
		public static async Task<Contract.Activity?> ConvertToGhostfolioActivity(
			IExchangeRateService exchangeRateService,
			Model.Symbols.SymbolProfile? symbolProfile,
			Model.Activities.Activity activity)
		{
			async Task<decimal> CalculateFeeAndTaxes(IEnumerable<Money> fees, IEnumerable<Money> taxes, Currency targetCurrency, DateTime dateTime)
			{
				decimal amount = 0;

				foreach (var money in fees.Union(taxes))
				{
					amount += await ConvertPrice(exchangeRateService, money, targetCurrency, dateTime);
				}

				return amount;
			}

			if (activity.ActivityType == Model.Activities.ActivityType.CashConvert ||
				activity.ActivityType == Model.Activities.ActivityType.CashDeposit ||
				activity.ActivityType == Model.Activities.ActivityType.CashWithdrawal ||
				activity.ActivityType == Model.Activities.ActivityType.KnownBalance ||
				activity.ActivityType == Model.Activities.ActivityType.Gift)
			{
				return null;
			}

			if (activity.ActivityType == Model.Activities.ActivityType.Interest ||
			activity.ActivityType == Model.Activities.ActivityType.Fee ||
			activity.ActivityType == Model.Activities.ActivityType.Valuable ||
			activity.ActivityType == Model.Activities.ActivityType.Liability)
			{
				return new Contract.Activity
				{
					AccountId = activity.Account.Id,
					SymbolProfile = Contract.SymbolProfile.Empty(activity.Account.Balance.Money.Currency, activity.Description),
					Comment = TransactionReferenceUtilities.GetComment(activity),
					Date = activity.Date,
					Fee = await CalculateFeeAndTaxes(activity.Fees, activity.Taxes, activity.Account.Balance.Money.Currency, activity.Date),
					FeeCurrency = activity.Account.Balance.Money.Currency.Symbol,
					Quantity = activity.Quantity,
					Type = ParseType(activity.ActivityType),
					UnitPrice = await ConvertPrice(exchangeRateService, activity.UnitPrice, activity.Account.Balance.Money.Currency, activity.Date),
					ReferenceCode = activity.TransactionId,
				};
			}

			if (symbolProfile == null)
			{
				throw new NotSupportedException("Activity unable to convert");
			}

			if (activity.ActivityType == Model.Activities.ActivityType.Dividend)
			{
				return new Contract.Activity
				{
					AccountId = activity.Account.Id,
					SymbolProfile = new Contract.SymbolProfile
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
					Fee = await CalculateFeeAndTaxes(activity.Fees, activity.Taxes, symbolProfile.Currency, activity.Date),
					FeeCurrency = symbolProfile.Currency.Symbol,
					Quantity = activity.Quantity * await exchangeRateService.GetConversionRate(activity.UnitPrice.Currency, symbolProfile.Currency, activity.Date),
					Type = ParseType(activity.ActivityType),
					UnitPrice = 1,
					ReferenceCode = activity.TransactionId
				};
			}

			return new Contract.Activity
			{
				AccountId = activity.Account.Id,
				SymbolProfile = new Contract.SymbolProfile
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
				Fee = await CalculateFeeAndTaxes(activity.Fees, activity.Taxes, symbolProfile.Currency, activity.Date),
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

		private static Contract.ActivityType ParseType(Model.Activities.ActivityType? type)
		{
			switch (type)
			{
				case null:
					return Contract.ActivityType.IGNORE;
				case Model.Activities.ActivityType.Buy:
					return Contract.ActivityType.BUY;
				case Model.Activities.ActivityType.Sell:
					return Contract.ActivityType.SELL;
				case Model.Activities.ActivityType.Dividend:
					return Contract.ActivityType.DIVIDEND;
				case Model.Activities.ActivityType.Send:
					return Contract.ActivityType.SELL;
				case Model.Activities.ActivityType.Receive:
					return Contract.ActivityType.BUY;
				case Model.Activities.ActivityType.Convert:
					return Contract.ActivityType.IGNORE;
				case Model.Activities.ActivityType.Interest:
					return Contract.ActivityType.INTEREST;
				case Model.Activities.ActivityType.Fee:
					return GhostfolioAPI.Contract.ActivityType.FEE;
				case Model.Activities.ActivityType.Valuable:
					return GhostfolioAPI.Contract.ActivityType.ITEM;
				case Model.Activities.ActivityType.Liability:
					return GhostfolioAPI.Contract.ActivityType.LIABILITY;
				case Model.Activities.ActivityType.Gift:
					return Contract.ActivityType.BUY;
				case Model.Activities.ActivityType.LearningReward:
					return Contract.ActivityType.IGNORE;
				case Model.Activities.ActivityType.StakingReward:
					return Contract.ActivityType.IGNORE;
				default:
					throw new NotSupportedException($"ActivityType {type} not supported");
			}
		}
	}
}
