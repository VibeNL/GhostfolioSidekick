using CsvHelper.Configuration.Attributes;
using GhostfolioSidekick.Model.Activities;
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

		[Name("Valutadatum")]
		[Format("dd-MM-yyyy")]
		public override DateOnly CurrencyDate { get; set; }

		[Name("Product")]
		public override string? Product { get; set; }

		[Name("ISIN")]
		public override string? ISIN { get; set; }

		[Name("Omschrijving")]
		public override string? Description { get; set; }

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
			if (Description == null)
			{
				return null;
			}

			if (Description == "DEGIRO Transactiekosten en/of kosten van derden")
			{
				return ActivityType.Fee;
			}

			if (Description == "Dividendbelasting")
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
				return ActivityType.CashDeposit; // TODO: Gift?
			}

			// TODO, implement other options
			return null;
		}

		public override decimal GetQuantity()
		{
			// oop is the same for both buy and sell or Koop and Verkoop in dutch
			// dont include currency at the end, this can be other things than EUR
			var quantity = Regex.Match(Description!, $"oop (?<amount>\\d+) @ (?<price>[0-9]+[,0-9]+)").Groups[1].Value;

			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		public override decimal GetUnitPrice()
		{
			// oop is the same for both buy and sell or Koop and Verkoop in dutch
			// dont include currency at the end, this can be other things than EUR
			var quantity = Regex.Match(Description!, $"oop (?<amount>\\d+) @ (?<price>[0-9]+[,0-9]+)").Groups[2].Value;

			return decimal.Parse(quantity, GetCultureForParsingNumbers());
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
