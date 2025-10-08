using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	internal class DeGiroMultiStrategy(params IDeGiroStrategy[] strategies) : IDeGiroStrategy
	{
		public PartialActivityType? GetActivityType(DeGiroRecord record)
		{
			foreach (var strategy in strategies)
			{
				var activityType = strategy.GetActivityType(record);
				if (activityType != null)
				{
					return activityType;
				}
			}

			return null;
		}

		public Currency GetCurrency(DeGiroRecord record, ICurrencyMapper currencyMapper)
		{
			foreach (var strategy in strategies.Where(s => s.GetActivityType(record) != null))
			{
				var currency = strategy.GetCurrency(record, currencyMapper);
				if (currency != null)
				{
					return currency;
				}
			}

			return Currency.EUR;
		}

		public decimal GetQuantity(DeGiroRecord record)
		{
			foreach (var strategy in strategies.Where(s => s.GetActivityType(record) != null))
			{
				var quantity = strategy.GetQuantity(record);
				if (quantity != 0)
				{
					return quantity;
				}
			}

			return 0;
		}

		public decimal GetUnitPrice(DeGiroRecord record)
		{
			foreach (var strategy in strategies.Where(s => s.GetActivityType(record) != null))
			{
				var unitPrice = strategy.GetUnitPrice(record);
				if (unitPrice != 0)
				{
					return unitPrice;
				}
			}

			return 0;
		}

		public void SetGenerateTransactionIdIfEmpty(DeGiroRecord record, DateTime recordDate)
		{
			var strategy = strategies.FirstOrDefault(s => s.GetActivityType(record) != null);
			if (strategy != null)
			{
				strategy.SetGenerateTransactionIdIfEmpty(record, recordDate);
				return;
			}

			strategies.FirstOrDefault()?.SetGenerateTransactionIdIfEmpty(record, recordDate);
		}
	}
}