using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities.Types;

namespace GhostfolioSidekick.GhostfolioAPI.API.Mapper
{
	public static class ModelToContractMapper
	{
		public static async Task<Contract.Activity?> ConvertToGhostfolioActivity(
			ICurrencyExchange exchangeRateService,
			Contract.SymbolProfile? symbolProfile,
			Model.Activities.Activity activity,
			Contract.Account? account)
		{
			async Task<decimal> CalculateFeeAndTaxes(IEnumerable<Money> fees, IEnumerable<Money> taxes, string targetCurrency, DateTime dateTime)
			{
				decimal amount = 0;

				foreach (var money in fees.Concat(taxes))
				{
					amount += await ConvertPrice(exchangeRateService, money, targetCurrency, dateTime);
				}

				return amount;
			}

			switch (activity)
			{
				case BuyActivity buyActivity:
					if (symbolProfile == null)
					{
						return null;
					}

					return new Contract.Activity
					{
						SymbolProfile = symbolProfile!,
						Comment = TransactionReferenceUtilities.GetComment(activity, symbolProfile),
						Date = activity.Date,
						Fee = await CalculateFeeAndTaxes(buyActivity.Fees.Select(x => x.Money), buyActivity.Taxes.Select(x => x.Money), symbolProfile!.Currency, activity.Date),
						FeeCurrency = symbolProfile.Currency,
						Quantity = Math.Abs(buyActivity.AdjustedQuantity),
						Type = Contract.ActivityType.BUY,
						UnitPrice = await ConvertPrice(exchangeRateService, buyActivity.AdjustedUnitPrice, symbolProfile.Currency, activity.Date),
						ReferenceCode = activity.TransactionId,
						AccountId = account?.Id
					};
				case SellActivity sellActivity:
					if (symbolProfile == null)
					{
						return null;
					}

					return new Contract.Activity
					{
						SymbolProfile = symbolProfile!,
						Comment = TransactionReferenceUtilities.GetComment(activity, symbolProfile),
						Date = activity.Date,
						Fee = await CalculateFeeAndTaxes(sellActivity.Fees.Select(x => x.Money), sellActivity.Taxes.Select(x => x.Money), symbolProfile!.Currency, activity.Date),
						FeeCurrency = symbolProfile.Currency,
						Quantity = Math.Abs(sellActivity.AdjustedQuantity),
						Type = Contract.ActivityType.SELL,
						UnitPrice = await ConvertPrice(exchangeRateService, sellActivity.AdjustedUnitPrice, symbolProfile.Currency, activity.Date),
						ReferenceCode = activity.TransactionId,
						AccountId = account?.Id
					};
				case ReceiveActivity receiveActivity:
					if (symbolProfile == null)
					{
						return null;
					}

					return new Contract.Activity
					{
						SymbolProfile = symbolProfile!,
						Comment = TransactionReferenceUtilities.GetComment(activity, symbolProfile),
						Date = activity.Date,
						Fee = await CalculateFeeAndTaxes([], [], symbolProfile!.Currency, activity.Date),
						FeeCurrency = symbolProfile.Currency,
						Quantity = Math.Abs(receiveActivity.AdjustedQuantity),
						Type = Contract.ActivityType.BUY,
						UnitPrice = await ConvertPrice(exchangeRateService, receiveActivity.AdjustedUnitPrice, symbolProfile.Currency, activity.Date),
						ReferenceCode = activity.TransactionId,
						AccountId = account?.Id
					};
				case SendActivity sendAndReceiveActivity:
					if (symbolProfile == null)
					{
						return null;
					}

					return new Contract.Activity
					{
						SymbolProfile = symbolProfile!,
						Comment = TransactionReferenceUtilities.GetComment(activity, symbolProfile),
						Date = activity.Date,
						Fee = await CalculateFeeAndTaxes(sendAndReceiveActivity.Fees.Select(x => x.Money), [], symbolProfile!.Currency, activity.Date),
						FeeCurrency = symbolProfile.Currency,
						Quantity = Math.Abs(sendAndReceiveActivity.AdjustedQuantity),
						Type = Contract.ActivityType.SELL,
						UnitPrice = await ConvertPrice(exchangeRateService, sendAndReceiveActivity.AdjustedUnitPrice, symbolProfile.Currency, activity.Date),
						ReferenceCode = activity.TransactionId,
						AccountId = account?.Id
					};
				case DividendActivity dividendActivity:
					if (symbolProfile == null)
					{
						return null;
					}

					return new Contract.Activity
					{
						SymbolProfile = symbolProfile!,
						Comment = TransactionReferenceUtilities.GetComment(activity, symbolProfile),
						Date = activity.Date,
						Fee = await CalculateFeeAndTaxes(dividendActivity.Fees.Select(x => x.Money), dividendActivity.Taxes.Select(x => x.Money), symbolProfile!.Currency, activity.Date),
						FeeCurrency = symbolProfile.Currency,
						Quantity = 1,
						Type = Contract.ActivityType.DIVIDEND,
						UnitPrice = await ConvertPrice(exchangeRateService, dividendActivity.Amount, symbolProfile.Currency, activity.Date),
						ReferenceCode = activity.TransactionId,
						AccountId = account?.Id
					};
				case InterestActivity interestActivity:
					return new Contract.Activity
					{
						SymbolProfile = Contract.SymbolProfile.Empty(interestActivity.Amount.Currency, interestActivity.Description),
						Comment = TransactionReferenceUtilities.GetComment(activity),
						Date = activity.Date,
						Quantity = 1,
						Type = Contract.ActivityType.INTEREST,
						UnitPrice = interestActivity.Amount.Amount,
						ReferenceCode = activity.TransactionId,
						AccountId = account?.Id
					};
				case FeeActivity feeActivity:
					return new Contract.Activity
					{
						SymbolProfile = Contract.SymbolProfile.Empty(feeActivity.Amount.Currency, feeActivity.Description),
						Comment = TransactionReferenceUtilities.GetComment(activity),
						Date = activity.Date,
						Quantity = 1,
						Type = Contract.ActivityType.FEE,
						UnitPrice = feeActivity.Amount.Amount,
						ReferenceCode = activity.TransactionId,
						AccountId = account?.Id
					};
				case ValuableActivity valuableActivity:
					return new Contract.Activity
					{
						SymbolProfile = Contract.SymbolProfile.Empty(valuableActivity.Amount.Currency, valuableActivity.Description),
						Comment = TransactionReferenceUtilities.GetComment(activity),
						Date = activity.Date,
						Quantity = 1,
						Type = Contract.ActivityType.ITEM,
						UnitPrice = valuableActivity.Amount.Amount,
						ReferenceCode = activity.TransactionId,
						AccountId = account?.Id
					};
				case LiabilityActivity liabilityActivity:
					return new Contract.Activity
					{
						SymbolProfile = Contract.SymbolProfile.Empty(liabilityActivity.Amount.Currency, liabilityActivity.Description),
						Comment = TransactionReferenceUtilities.GetComment(activity),
						Date = activity.Date,
						Quantity = 1,
						Type = Contract.ActivityType.LIABILITY,
						UnitPrice = liabilityActivity.Amount.Amount,
						ReferenceCode = activity.TransactionId,
						AccountId = account?.Id
					};
				case KnownBalanceActivity:
				case CashDepositActivity:
				case CashWithdrawalActivity:
				case StakingRewardActivity:
					return null;
			}

			throw new NotSupportedException($"{activity.GetType().Name} not supported in ModelToContractMapper");
		}

		private static async Task<decimal> ConvertPrice(ICurrencyExchange exchangeRateService, Money? money, string targetCurrency, DateTime dateTime)
		{
			if (money == null)
			{
				return 0;
			}

			return (await exchangeRateService.ConvertMoney(money, Currency.GetCurrency(targetCurrency), DateOnly.FromDateTime(dateTime))).Amount;
		}
	}
}