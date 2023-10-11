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

		public Contract.Activity ConvertToGhostfolioActivity(Account account, Activity activity)
		{
			decimal Round(decimal? value)
			{
				return Math.Round(value ?? 0, 10);
			};

			if (activity.ActivityType == ActivityType.Interest)
			{
				return new Contract.Activity
				{
					AccountId = account.Id,
					Currency = account.Balance.Currency.Symbol,
					Asset = null,
					Comment = activity.Comment,
					Date = activity.Date,
					Fee = Round((currentPriceCalculator.GetConvertedPrice(activity.Fee, account.Balance.Currency, activity.Date))?.Amount),
					Quantity = Round(activity.Quantity),
					Type = ParseType(activity.ActivityType),
					UnitPrice = Round((currentPriceCalculator.GetConvertedPrice(activity.UnitPrice, account.Balance.Currency, activity.Date)).Amount),
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
				Asset = new Contract.Asset
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
				Fee = Round((currentPriceCalculator.GetConvertedPrice(activity.Fee, activity.Asset.Currency, activity.Date))?.Amount),
				Quantity = Round(activity.Quantity),
				Type = ParseType(activity.ActivityType),
				UnitPrice = Round((currentPriceCalculator.GetConvertedPrice(activity.UnitPrice, activity.Asset.Currency, activity.Date)).Amount),
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
