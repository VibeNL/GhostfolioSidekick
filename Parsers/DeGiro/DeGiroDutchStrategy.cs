using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	public class DeGiroDutchStrategy : IDeGiroStrategy
	{
		public PartialActivityType? GetActivityType(DeGiroRecord record)
		{
			if (record.Description.Equals("DEGIRO Transactiekosten en/of kosten van derden"))
			{
				return PartialActivityType.Fee;
			}

			if (record.Description.Equals("Dividendbelasting"))
			{
				return PartialActivityType.Tax;
			}

			if (record.Description.Contains("Verkoop")) // check Verkoop first because otherwise koop get's triggered
			{
				return PartialActivityType.Sell;
			}

			if (record.Description.Contains("Koop"))
			{
				return PartialActivityType.Buy;
			}

			if (record.Description.Equals("Dividend"))
			{
				return PartialActivityType.Dividend;
			}

			if (record.Description.Equals("flatex terugstorting"))
			{
				return PartialActivityType.CashWithdrawal;
			}

			if (record.Description.Contains("Deposit") && !record.Description.Contains("Reservation"))
			{
				return PartialActivityType.CashDeposit;
			}

			if (record.Description.Equals("DEGIRO Verrekening Promotie"))
			{
				return PartialActivityType.CashDeposit;
			}

			return null;
		}

		public decimal GetQuantity(DeGiroRecord record)
		{
			// oop is the same for both buy and sell or Koop and Verkoop in dutch
			// dont include currency at the end, this can be other things than EUR
			var quantity = Regex.Match(record.Description!, "oop (?<amount>\\d+) @ (?<price>[0-9]+[,0-9]+) (?<currency>[A-Z]+)", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Groups[1].Value;

			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		public decimal GetUnitPrice(DeGiroRecord record)
		{
			// oop is the same for both buy and sell or Koop and Verkoop in dutch
			// dont include currency at the end, this can be other things than EUR
			var quantity = Regex.Match(record.Description!, "oop (?<amount>\\d+) @ (?<price>[0-9]+[,0-9]+) (?<currency>[A-Z]+)", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Groups[2].Value;

			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		public Currency GetCurrency(DeGiroRecord record, ICurrencyMapper currencyMapper)
		{
			var currency = Regex.Match(record.Description!, "oop (?<amount>\\d+) @ (?<price>[0-9]+[,0-9]+) (?<currency>[A-Z]+)", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Groups[3].Value;

			return currencyMapper.Map(currency);
		}

		public decimal? GetTotal(DeGiroRecord record)
		{
			if (string.IsNullOrWhiteSpace(record.Total))
			{
				return null;
			}

			return decimal.Parse(record.Total, GetCultureForParsingNumbers());
		}

		public decimal GetBalance(DeGiroRecord record)
		{
			return decimal.Parse(record.Balance, GetCultureForParsingNumbers());
		}

		public void SetGenerateTransactionIdIfEmpty(DeGiroRecord record, DateTime recordDate)
		{
			if (!string.IsNullOrWhiteSpace(record.TransactionId))
			{
				return;
			}

			var activity = GetActivityType(record);
			var mutation = record.Mutation;
			const string dividendText = "Dividend";
			if (record.Description.StartsWith(dividendText))
			{
				mutation = dividendText;
				activity = PartialActivityType.Dividend;
			}

			record.TransactionId = $"{activity}_{recordDate.ToInvariantString()}_{record.Product}_{record.ISIN}_{mutation}";
		}

		private static CultureInfo GetCultureForParsingNumbers()
		{
			return new CultureInfo("en")
			{
				NumberFormat =
				{
					NumberDecimalSeparator = ","
				}
			};
		}
	}
}
