﻿namespace GhostfolioSidekick.Model
{
	public class Activity
	{
		public Activity()
		{
			Fees = Enumerable.Empty<Money>();
		}

		public Activity(ActivityType activityType, SymbolProfile? asset, DateTime date, decimal quantity, Money unitPrice, IEnumerable<Money>? fees, string comment, string referenceCode)
		{
			Asset = asset;
			Comment = comment;
			Date = date;
			Fees = fees ?? Enumerable.Empty<Money>();
			Quantity = quantity;
			ActivityType = activityType;
			UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));
			ReferenceCode = referenceCode;
		}

		public SymbolProfile? Asset { get; set; }

		public string Comment { get; set; }

		public DateTime Date { get; set; }

		public IEnumerable<Money> Fees { get; set; }

		public decimal Quantity { get; set; }

		public ActivityType ActivityType { get; set; }

		public Money UnitPrice { get; set; }

		// Internal use
		public string ReferenceCode { get; set; }

		internal static Activity GetKnownBalance(Money money)
		{
			return new Activity
			{
				ActivityType = ActivityType.CashBalance,
				Quantity = 1,
				UnitPrice = money
			};
		}
	}
}