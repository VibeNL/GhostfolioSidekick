using CsvHelper.Configuration.Attributes;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	[Delimiter(",")]
	public class DeGiroRecordNL : DeGiroRecordBase
	{
		[Format("dd-MM-yyyy")]
		[Name("Datum")]
		public override DateOnly Date { get; set; }

		[Name("Tijd")]
		public override TimeOnly Time { get; set; }

		[ExcludeFromCodeCoverage]
		[Name("Valutadatum")]
		[Format("dd-MM-yyyy")]
		public override DateOnly CurrencyDate { get; set; }

		[Name("Product")]
		public override string? Product { get; set; }

		[Name("ISIN")]
		public override string? ISIN { get; set; }

		[Name("Omschrijving")]
		public override required string Description { get; set; }

		[ExcludeFromCodeCoverage]
		[Name("FX")]
		public override string? FX { get; set; }

		[Name("Mutatie")]
		public override required string Mutation { get; set; }

		[Index(8)]
		public override decimal? Total { get; set; }

		[Name("Saldo")]
		public override required string BalanceCurrency { get; set; }

		[Index(10)]
		public override decimal Balance { get; set; }

		[Name("Order Id")]
		public override string? TransactionId { get; set; }

		public override ActivityType? GetActivityType()
		{
			if (Description.Equals("DEGIRO Transactiekosten en/of kosten van derden"))
			{
				return ActivityType.Fee;
			}

			if (Description.Equals("Dividendbelasting"))
			{
				return ActivityType.Tax;
			}

			if (Description.Contains("Verkoop")) // check Verkoop first because otherwise koop get's triggered
			{
				return ActivityType.Sell;
			}

			if (Description.Contains("Koop"))
			{
				return ActivityType.Buy;
			}

			if (Description.Equals("Dividend"))
			{
				return ActivityType.Dividend;
			}

			if (Description.Equals("flatex terugstorting"))
			{
				return ActivityType.CashWithdrawal;
			}

			if (Description.Contains("Deposit") && !Description.Contains("Reservation"))
			{
				return ActivityType.CashDeposit;
			}

			if (Description.Equals("DEGIRO Verrekening Promotie"))
			{
				return ActivityType.CashDeposit;
			}

			return null;
		}

		public override decimal GetQuantity()
		{
			// oop is the same for both buy and sell or Koop and Verkoop in dutch
			// dont include currency at the end, this can be other things than EUR
			var quantity = Regex.Match(Description!, "oop (?<amount>\\d+) @ (?<price>[0-9]+[,0-9]+) (?<currency>[A-Z]+)").Groups[1].Value;

			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		public override decimal GetUnitPrice()
		{
			// oop is the same for both buy and sell or Koop and Verkoop in dutch
			// dont include currency at the end, this can be other things than EUR
			var quantity = Regex.Match(Description!, "oop (?<amount>\\d+) @ (?<price>[0-9]+[,0-9]+) (?<currency>[A-Z]+)").Groups[2].Value;

			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		public override Currency GetCurrency(ICurrencyMapper currencyMapper)
		{
			var currency = Regex.Match(Description!, "oop (?<amount>\\d+) @ (?<price>[0-9]+[,0-9]+) (?<currency>[A-Z]+)").Groups[3].Value;

			return currencyMapper.Map(currency);
		}

		public override void SetGenerateTransactionIdIfEmpty(DateTime recordDate)
		{
			if (!string.IsNullOrWhiteSpace(TransactionId))
			{
				return;
			}

			var activity = GetActivityType();
			var mutation = Mutation;
			const string dividendText = "Dividend";
			if (Description.StartsWith(dividendText))
			{
				mutation = dividendText;
				activity = ActivityType.Dividend;
			}

			TransactionId = $"{activity}_{recordDate.ToInvariantString()}_{Product}_{ISIN}_{mutation}";
		}

		private CultureInfo GetCultureForParsingNumbers()
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
