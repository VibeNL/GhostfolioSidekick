using CsvHelper.Configuration.Attributes;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	public class DeGiroEnglishStrategy : IDeGiroStrategy
	{
		private const string Pattern = "[Buy|Sell] (?<amount>\\d+) (.*)@(?<price>[0-9]+[.0-9]+) (?<currency>[A-Z]+)";
		private const string PatternMoneyMarketfunds = "Money Market fund conversion: (Buy|Sell) (?<amount>[.0-9]*) at (?<price>[.0-9]*) (?<currency>[A-Z]+)";
		
		public PartialActivityType? GetActivityType(DeGiroRecord record)
		{
			if (record.Description.Equals("DEGIRO Transaction and/or third party fees"))
			{
				return PartialActivityType.Fee;
			}

			if (record.Description.Equals("Dividend Tax"))
			{
				return PartialActivityType.Tax;
			}

			if (record.Description.Contains("Sell"))
			{
				return PartialActivityType.Sell;
			}

			if (record.Description.Contains("Buy"))
			{
				return PartialActivityType.Buy;
			}

			if (record.Description.Equals("Dividend") || record.Description.Equals("Fund Distribution"))
			{
				return PartialActivityType.Dividend;
			}

			
			return null;
		}

		public decimal GetQuantity(DeGiroRecord record)
		{
			return decimal.Parse(GetValue(record, 2), GetCultureForParsingNumbers());
		}

		
		public decimal GetUnitPrice(DeGiroRecord record)
		{
			return decimal.Parse(GetValue(record, 3), GetCultureForParsingNumbers());
		}

		public Currency GetCurrency(DeGiroRecord record, ICurrencyMapper currencyMapper)
		{
			return currencyMapper.Map(GetValue(record, 4));
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
					NumberDecimalSeparator = "."
				}
			};
		}

		private static string GetValue(DeGiroRecord record, int group)
		{
			string quantity;
			if (Regex.IsMatch(record.Description!, PatternMoneyMarketfunds, RegexOptions.None, TimeSpan.FromMilliseconds(100)))
			{
				quantity = Regex.Match(record.Description!, PatternMoneyMarketfunds, RegexOptions.None, TimeSpan.FromMilliseconds(100)).Groups[group].Value;
				return quantity;
			}

			quantity = Regex.Match(record.Description!, Pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100)).Groups[group].Value;
			return quantity;
		}
	}
}
