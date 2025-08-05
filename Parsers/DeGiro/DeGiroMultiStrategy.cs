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

		public decimal GetBalance(DeGiroRecord record)
		{
			foreach (var strategy in strategies)
			{
				var balance = strategy.GetBalance(record);
				if (balance != 0)
				{
					return balance;
				}
			}

			return 0;
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

			return Currency.EUR;
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

			return 0;
		}

		public decimal? GetTotal(DeGiroRecord record)
		{
			foreach (var strategy in strategies)
			{
				var total = strategy.GetTotal(record);
				if (total != null)
				{
					return total;
				}
			}

			return null;
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

			return 0;
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