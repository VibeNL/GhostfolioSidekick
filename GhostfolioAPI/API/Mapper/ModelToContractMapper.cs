//using GhostfolioSidekick.GhostfolioAPI;
//using GhostfolioSidekick.Model;

//namespace GhostfolioSidekick.Ghostfolio.API.Mapper
//{
//	public class ModelToContractMapper(GhostfolioAPI.API.IExchangeRateService exchangeRateService)
//	{
//		public GhostfolioAPI.Contract.Activity? ConvertToGhostfolioActivity(
//			Model.Accounts.Account account, 
//			Model.Activities.Activity activity)
//		{
//			decimal CalculateFee(IEnumerable<Money> fees, Currency targetCurrency, DateTime dateTime)
//			{
//				decimal amount = 0;

//				foreach (var fee in fees)
//				{
//					var rate = exchangeRateService.GetConversionRate(fee.Currency, targetCurrency, dateTime);

//					amount += rate * fee.Amount;
//				}

//				return amount;
//			}

//			if (activity.ActivityType == Model.Activities.ActivityType.Interest ||
//				activity.ActivityType == Model.Activities.ActivityType.Fee)
//			{
//				return new GhostfolioAPI.Contract.Activity
//				{
//					AccountId = account.Id,
//					Currency = account.Balance.Money.Currency.Symbol,
//					SymbolProfile = null,
//					Comment = TransactionReferenceUtilities.GetComment(activity.TransactionId),
//					Date = activity.Date,
//					Fee = CalculateFee(activity.Fees, account.Balance.Money.Currency, activity.Date),
//					FeeCurrency = account.Balance.Money.Currency.Symbol,
//					Quantity = activity.Quantity,
//					Type = ParseType(activity.ActivityType),
//					UnitPrice = currentPriceCalculator.GetConvertedPrice(activity.UnitPrice, account.Balance.Money.Currency, activity.Date)!.Amount,
//					ReferenceCode = activity.TransactionId
//				};
//			}

//			if (activity.Asset == null)
//			{
//				return null;
//			}

//			return new GhostfolioAPI.Contract.Activity
//            {
//				AccountId = account.Id,
//				Currency = activity.Asset.Currency?.Symbol,
//				SymbolProfile = new Contract.SymbolProfile
//				{
//					Symbol = activity.Asset.Symbol,
//					AssetClass = activity.Asset.AssetClass.ToString(),
//					AssetSubClass = activity.Asset.AssetSubClass?.ToString(),
//					Currency = activity.Asset.Currency!.Symbol,
//					DataSource = activity.Asset.DataSource!,
//					Name = activity.Asset.Name
//				},
//				Comment = activity.Comment,
//				Date = activity.Date,
//				Fee = CalculateFee(activity.Fees, activity.Asset.Currency),
//				FeeCurrency = activity.Asset.Currency?.Symbol,
//				Quantity = activity.Quantity,
//				Type = ParseType(activity.ActivityType),
//				UnitPrice = currentPriceCalculator.GetConvertedPrice(activity.UnitPrice, activity.Asset.Currency!, activity.Date)?.Amount ?? 0,
//				ReferenceCode = activity.TransactionId
//			};
//		}

//		private GhostfolioAPI.Contract.ActivityType ParseType(Model.Activities.ActivityType? type)
//		{
//			switch (type)
//			{
//				case null:
//					return GhostfolioAPI.Contract.ActivityType.IGNORE;
//				case Model.Activities.ActivityType.Buy:
//					return GhostfolioAPI.Contract.ActivityType.BUY;
//				case Model.Activities.ActivityType.Sell:
//					return GhostfolioAPI.Contract.ActivityType.SELL;
//				case Model.Activities.ActivityType.Dividend:
//					return GhostfolioAPI.Contract.ActivityType.DIVIDEND;
//				case Model.Activities.ActivityType.Send:
//					return GhostfolioAPI.Contract.ActivityType.SELL;
//				case Model.Activities.ActivityType.Receive:
//					return GhostfolioAPI.Contract.ActivityType.BUY;
//				case Model.Activities.ActivityType.Convert:
//					return GhostfolioAPI.Contract.ActivityType.IGNORE;
//				case Model.Activities.ActivityType.Interest:
//					return GhostfolioAPI.Contract.ActivityType.INTEREST;
//				case Model.Activities.ActivityType.Fee:
//					return GhostfolioAPI.Contract.ActivityType.FEE;
//				case Model.Activities.ActivityType.Gift:
//					return GhostfolioAPI.Contract.ActivityType.BUY;
//				case Model.Activities.ActivityType.LearningReward:
//					return GhostfolioAPI.Contract.ActivityType.IGNORE;
//				case Model.Activities.ActivityType.StakingReward:
//					return GhostfolioAPI.Contract.ActivityType.IGNORE;
//				default:
//					throw new NotSupportedException($"ActivityType {type} not supported");
//			}
//		}
//	}
//}
