using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Ghostfolio.API.Mapper
{
	public class ModelToContractMapper
	{
		private readonly ICurrentPriceCalculator currentPriceCalculator;

		public ModelToContractMapper(ICurrentPriceCalculator currentPriceCalculator)
		{
			this.currentPriceCalculator = currentPriceCalculator;
		}

		public Ghostfolio.Contract.Activity ConvertToGhostfolioActivity(Model.Account account, Model.Activity activity)
		{
			if (activity.ActivityType == Model.ActivityType.Interest)
			{
				return new Contract.Activity
				{
					AccountId = account.Id,
					Currency = account.Balance.Currency.Symbol,
					SymbolProfile = null,
					Comment = activity.Comment,
					Date = activity.Date,
					Fee = currentPriceCalculator.GetConvertedPrice(activity.Fee, account.Balance.Currency, activity.Date)?.Amount ?? 0,
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
					AssetClass = activity.Asset.AssetClass,
					AssetSubClass = activity.Asset.AssetSubClass,
					Currency = activity.Asset.Currency.Symbol,
					DataSource = activity.Asset.DataSource,
					Name = activity.Asset.Name
				},
				Comment = activity.Comment,
				Date = activity.Date,
				Fee = currentPriceCalculator.GetConvertedPrice(activity.Fee, activity.Asset.Currency, activity.Date)?.Amount ?? 0,
				Quantity = activity.Quantity,
				Type = ParseType(activity.ActivityType),
				UnitPrice = currentPriceCalculator.GetConvertedPrice(activity.UnitPrice, activity.Asset.Currency, activity.Date).Amount,
				ReferenceCode = activity.ReferenceCode
			};
		}

		private Ghostfolio.Contract.ActivityType ParseType(Model.ActivityType? type)
		{
			switch (type)
			{
				case null:
					return Ghostfolio.Contract.ActivityType.IGNORE;
				case Model.ActivityType.Buy:
					return Ghostfolio.Contract.ActivityType.BUY;
				case Model.ActivityType.Sell:
					return Ghostfolio.Contract.ActivityType.SELL;
				case Model.ActivityType.Dividend:
					return Ghostfolio.Contract.ActivityType.DIVIDEND;
				case Model.ActivityType.Send:
					return Ghostfolio.Contract.ActivityType.SELL; // TODO: 
				case Model.ActivityType.Receive:
					return Ghostfolio.Contract.ActivityType.BUY; // TODO: 
				case Model.ActivityType.Convert:
					return Ghostfolio.Contract.ActivityType.IGNORE; // TODO: 
				case Model.ActivityType.Interest:
					return Ghostfolio.Contract.ActivityType.INTEREST;
				case Model.ActivityType.Gift:
					return Ghostfolio.Contract.ActivityType.BUY; // TODO: 
				case Model.ActivityType.LearningReward:
					return Ghostfolio.Contract.ActivityType.IGNORE; // TODO: 
				case Model.ActivityType.StakingReward:
					return Ghostfolio.Contract.ActivityType.IGNORE; // TODO: 
				default:
					throw new NotSupportedException($"ActivityType {type} not supported");
			}
		}
	}
}
