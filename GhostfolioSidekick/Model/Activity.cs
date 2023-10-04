namespace GhostfolioSidekick.Model
{
	public class Activity
	{
		public Activity()
		{
		}

		public Activity(ActivityType type, Asset? asset, DateTime date, decimal quantity, Money unitPrice, Money fee, string comment, string referenceCode)
		{
			Asset = asset ?? throw new ArgumentNullException(nameof(asset));
			Comment = comment ?? throw new ArgumentNullException(nameof(comment));
			Date = date;
			Fee = fee ?? throw new ArgumentNullException(nameof(fee));
			Quantity = quantity;
			Type = type;
			UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));
			ReferenceCode = referenceCode ?? throw new ArgumentNullException(nameof(referenceCode));
		}

		public Asset? Asset { get; set; }

		public string Comment { get; set; }

		public DateTime Date { get; set; }

		public Money Fee { get; set; }

		public decimal Quantity { get; set; }

		public ActivityType Type { get; set; }

		public Money UnitPrice { get; set; }

		// Internal use
		public string ReferenceCode { get; set; }
	}
}