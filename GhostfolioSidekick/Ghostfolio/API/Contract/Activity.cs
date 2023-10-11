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

		internal void Union(Activity activity)
		{
			if (activity == null) throw new ArgumentNullException();

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

			if ((activity.Quantity + Quantity) != 0)
			{
				UnitPrice =
					((UnitPrice * Quantity * positiveOrNegativeThis) + (activity.UnitPrice * activity.Quantity * positiveOrNegativeOther))
					/
					(Quantity + activity.Quantity);
			}

			Quantity += positiveOrNegativeThis * activity.Quantity;
			Fee += positiveOrNegativeOther * activity.Fee;
		}
	}
}