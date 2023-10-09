namespace GhostfolioSidekick.Model
{
	public class Activity
	{
		public Activity()
		{
		}

		public Activity(ActivityType activityType, Asset? asset, DateTime date, decimal quantity, Money unitPrice, Money? fee, string comment, string referenceCode)
		{
			Asset = asset;
			Comment = comment;
			Date = date;
			Fee = fee;
			Quantity = quantity;
			ActivityType = activityType;
			UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));
			ReferenceCode = referenceCode;
		}

		public Asset? Asset { get; set; }

		public string Comment { get; set; }

		public DateTime Date { get; set; }

		public Money? Fee { get; set; }

		public decimal Quantity { get; set; }

		public ActivityType ActivityType { get; set; }

		public Money UnitPrice { get; set; }

		// Internal use
		public string ReferenceCode { get; set; }
	}
}