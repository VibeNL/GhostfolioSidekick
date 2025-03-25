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
			foreach (var strategy in strategies)
			{
				if (strategy.GetActivityType(record) == null)
				{
					continue;
				}

				var currency = strategy.GetCurrency(record, currencyMapper);
				if (currency != null)
				{
					return currency;
				}
			}

			throw new NotSupportedException();
		}

		public decimal GetQuantity(DeGiroRecord record)
		{
			foreach (var strategy in strategies)
			{
				if (strategy.GetActivityType(record) == null)
				{
					continue;
				}

				var quantity = strategy.GetQuantity(record);
				if (quantity != 0)
				{
					return quantity;
				}
			}

			throw new NotSupportedException();
		}

		public decimal GetUnitPrice(DeGiroRecord record)
		{
			foreach (var strategy in strategies)
			{
				if (strategy.GetActivityType(record) == null)
				{
					continue;
				}

				var unitPrice = strategy.GetUnitPrice(record);
				if (unitPrice != 0)
				{
					return unitPrice;
				}
			}

			throw new NotSupportedException();
		}

		public void SetGenerateTransactionIdIfEmpty(DeGiroRecord record, DateTime recordDate)
		{
			foreach (var strategy in strategies)
			{
				if (strategy.GetActivityType(record) != null)
				{
					strategy.SetGenerateTransactionIdIfEmpty(record, recordDate);
					return;
				}
			}

			strategies.FirstOrDefault()?.SetGenerateTransactionIdIfEmpty(record, recordDate);
		}
	}
}