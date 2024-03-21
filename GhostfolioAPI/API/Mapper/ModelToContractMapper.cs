using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Compare;

namespace GhostfolioSidekick.GhostfolioAPI.API.Mapper
{
	public static class ModelToContractMapper
	{
		public static async Task<Contract.Activity> ConvertToGhostfolioActivity(
			IExchangeRateService exchangeRateService,
			Model.Symbols.SymbolProfile? symbolProfile,
			Model.Activities.IActivity activity)
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

			switch (activity)
			{
				case BuySellActivity buyActivity:
					return new Contract.Activity
					{
						Id = activity.Id,
						AccountId = activity.Account.Id,
						SymbolProfile = CreateSymbolProfile(symbolProfile!),
						Comment = TransactionReferenceUtilities.GetComment(activity, symbolProfile),
						Date = activity.Date,
						Fee = await CalculateFeeAndTaxes(buyActivity.Fees, buyActivity.Taxes, symbolProfile!.Currency, activity.Date),
						FeeCurrency = symbolProfile.Currency.Symbol,
						Quantity = Math.Abs(buyActivity.Quantity),
						Type = buyActivity.Quantity > 0 ? Contract.ActivityType.BUY : Contract.ActivityType.SELL,
						UnitPrice = await ConvertPrice(exchangeRateService, buyActivity.UnitPrice, symbolProfile.Currency, activity.Date),
						ReferenceCode = activity.TransactionId
					};
				case DividendActivity dividendActivity:
					return new Contract.Activity
					{
						Id = activity.Id,
						AccountId = activity.Account.Id,
						SymbolProfile = CreateSymbolProfile(symbolProfile!),
						Comment = TransactionReferenceUtilities.GetComment(activity, symbolProfile),
						Date = activity.Date,
						Fee = await CalculateFeeAndTaxes(dividendActivity.Fees, dividendActivity.Taxes, symbolProfile!.Currency, activity.Date),
						FeeCurrency = symbolProfile.Currency.Symbol,
						Quantity = 1,
						Type = Contract.ActivityType.DIVIDEND,
						UnitPrice = (await exchangeRateService.GetConversionRate(dividendActivity.Amount?.Currency, symbolProfile.Currency, activity.Date)) * dividendActivity.Amount?.Amount ?? 0,
						ReferenceCode = activity.TransactionId
					};
				case InterestActivity interestActivity:
					return new Contract.Activity
					{
						Id = activity.Id,
						AccountId = activity.Account.Id,
						SymbolProfile = Contract.SymbolProfile.Empty(activity.Account.Balance.Money.Currency, interestActivity.Description),
						Comment = TransactionReferenceUtilities.GetComment(activity),
						Date = activity.Date,
						Quantity = 1,
						Type = Contract.ActivityType.INTEREST,
						UnitPrice = await ConvertPrice(exchangeRateService, interestActivity.Amount, activity.Account.Balance.Money.Currency, activity.Date),
						ReferenceCode = activity.TransactionId,
					};
				case FeeActivity feeActivity:
					return new Contract.Activity
					{
						Id = activity.Id,
						AccountId = activity.Account.Id,
						SymbolProfile = Contract.SymbolProfile.Empty(activity.Account.Balance.Money.Currency, feeActivity.Description),
						Comment = TransactionReferenceUtilities.GetComment(activity),
						Date = activity.Date,
						Quantity = 1,
						Type = Contract.ActivityType.FEE,
						UnitPrice = await ConvertPrice(exchangeRateService, feeActivity.Amount, activity.Account.Balance.Money.Currency, activity.Date),
						ReferenceCode = activity.TransactionId,
					};
				case ValuableActivity valuableActivity:
					return new Contract.Activity
					{
						Id = activity.Id,
						AccountId = activity.Account.Id,
						SymbolProfile = Contract.SymbolProfile.Empty(activity.Account.Balance.Money.Currency, valuableActivity.Description),
						Comment = TransactionReferenceUtilities.GetComment(activity),
						Date = activity.Date,
						Quantity = 1,
						Type = Contract.ActivityType.ITEM,
						UnitPrice = await ConvertPrice(exchangeRateService, valuableActivity.Price, activity.Account.Balance.Money.Currency, activity.Date),
						ReferenceCode = activity.TransactionId,
					};
				case LiabilityActivity liabilityActivity:
					return new Contract.Activity
					{
						Id = activity.Id,
						AccountId = activity.Account.Id,
						SymbolProfile = Contract.SymbolProfile.Empty(activity.Account.Balance.Money.Currency, liabilityActivity.Description),
						Comment = TransactionReferenceUtilities.GetComment(activity),
						Date = activity.Date,
						Quantity = 1,
						Type = Contract.ActivityType.LIABILITY,
						UnitPrice = await ConvertPrice(exchangeRateService, liabilityActivity.Price, activity.Account.Balance.Money.Currency, activity.Date),
						ReferenceCode = activity.TransactionId,
					};
				case GiftActivity giftActivity:
					return new Contract.Activity
					{
						Id = activity.Id,
						AccountId = activity.Account.Id,
						SymbolProfile = symbolProfile == null ? Contract.SymbolProfile.Empty(activity.Account.Balance.Money.Currency, giftActivity.Description) : CreateSymbolProfile(symbolProfile!),
						Comment = TransactionReferenceUtilities.GetComment(activity),
						Date = activity.Date,
						Quantity = giftActivity.Amount,
						Type = Contract.ActivityType.BUY,
						UnitPrice = await ConvertPrice(exchangeRateService, giftActivity.CalculatedUnitPrice, activity.Account.Balance.Money.Currency, activity.Date),
						ReferenceCode = activity.TransactionId,
					};
				case KnownBalanceActivity:
				case CashDepositWithdrawalActivity:
				case StockSplitActivity:
				case StakingRewardActivity:
					return new Contract.Activity
					{
						Type = Contract.ActivityType.IGNORE,
						SymbolProfile = Contract.SymbolProfile.Empty(activity.Account.Balance.Money.Currency, activity.Description),
					};
			}

			throw new NotSupportedException($"{activity.GetType().Name} not supported in ModelToContractMapper");

			static Contract.SymbolProfile CreateSymbolProfile(Model.Symbols.SymbolProfile symbolProfile)
			{
				return new Contract.SymbolProfile
				{
					Symbol = symbolProfile.Symbol,
					AssetClass = symbolProfile.AssetClass.ToString(),
					AssetSubClass = symbolProfile.AssetSubClass?.ToString(),
					Currency = symbolProfile.Currency.Symbol,
					DataSource = symbolProfile.DataSource.ToString(),
					Name = symbolProfile.Name,
					Countries = symbolProfile.Countries.Select(x => new Contract.Country { Code = x.Code, Continent = x.Continent, Name = x.Name, Weight = x.Weight }).ToArray(),
					Sectors = symbolProfile.Sectors.Select(x => new Contract.Sector { Name = x.Name, Weight = x.Weight }).ToArray()
				};
			}
		}

		private static async Task<decimal> ConvertPrice(IExchangeRateService exchangeRateService, Money? money, Currency targetCurrency, DateTime dateTime)
		{
			if (money == null)
			{
				return 0;
			}

			var rate = await exchangeRateService.GetConversionRate(money.Currency, targetCurrency, dateTime);
			return money.Amount * rate;
		}
	}
}
