namespace GhostfolioSidekick.Ghostfolio.API.Contract
{
	public class Activity
	{
		public string AccountId { get; set; }

		public Asset Asset { get; set; }

		public string Comment { get; set; }

		public string Currency { get; set; }

		public DateTime Date { get; set; }

		public decimal Fee { get; set; }

		public string FeeCurrency { get; set; }

		public decimal Quantity { get; set; }

		public ActivityType Type { get; set; }

		public decimal UnitPrice { get; set; }


		// Internal use
		public string ReferenceCode { get; set; }

		public Activity Merge(Activity activity)
		{
			if (activity == null) throw new ArgumentNullException();
			if (Type == ActivityType.IGNORE)
			{
				return this;
			}

			var canJoin =
				(Type == ActivityType.BUY || Type == ActivityType.SELL) &&
				(activity.Type == ActivityType.BUY || activity.Type == ActivityType.SELL) &&
				AccountId == activity.AccountId &&
				Asset.Symbol == activity.Asset.Symbol &&
				Currency == activity.Currency &&
				FeeCurrency == activity.FeeCurrency;

			if (!canJoin)
			{
				throw new NotSupportedException();
			}

			var positiveOrNegativeThis = Type == ActivityType.BUY ? 1 : -1;
			var positiveOrNegativeOther = activity.Type == ActivityType.BUY ? 1 : -1;

			decimal totalQuantity = Quantity + activity.Quantity;
			var unitPrice = totalQuantity == 0 ? 0 :
					((UnitPrice * Quantity) + (activity.UnitPrice * activity.Quantity))
					/
					totalQuantity;

			decimal quantity = positiveOrNegativeThis * Quantity + positiveOrNegativeOther * activity.Quantity;

			ActivityType activityType;
			if (quantity == 0)
			{
				activityType = ActivityType.IGNORE;
			}
			else if (quantity > 0)
			{
				activityType = ActivityType.BUY;
			}
			else
			{
				activityType = ActivityType.SELL;
			}

			return new Activity
			{
				Type = activityType,
				AccountId = AccountId,
				Asset = Asset,
				Comment = Comment,
				Currency = Currency,
				FeeCurrency = FeeCurrency,
				Date = Date,
				Fee = Fee + activity.Fee,
				Quantity = Math.Abs(quantity),
				ReferenceCode = ReferenceCode,
				UnitPrice = Math.Abs(unitPrice)
			};
		}
	}
}