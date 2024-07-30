﻿using CsvHelper.Configuration.Attributes;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	[Delimiter(",")]
	public class DeGiroRecordEN : DeGiroRecordBase
	{
		[Format("dd-MM-yyyy")]
		[Name("Date")]
		public override DateOnly Date { get; set; }

		[Name("Time")]
		public override TimeOnly Time { get; set; }

		[ExcludeFromCodeCoverage]
		[Name("Value date")]
		[Format("dd-MM-yyyy")]
		public override DateOnly CurrencyDate { get; set; }

		[Name("Product")]
		public override string? Product { get; set; }

		[Name("ISIN")]
		public override string? ISIN { get; set; }

		[Name("Description")]
		public override required string Description { get; set; }

		[ExcludeFromCodeCoverage]
		[Name("FX")]
		public override string? FX { get; set; }

		[Name("Change")]
		public override required string Mutation { get; set; }

		[Index(8)]
		public override decimal? Total { get; set; }

		[Name("Balance")]
		public override required string BalanceCurrency { get; set; }

		[Index(10)]
		public override decimal Balance { get; set; }

		[Name("Order Id")]
		public override string? TransactionId { get; set; }

		public override PartialActivityType? GetActivityType()
		{
			if (Description.Equals("DEGIRO Transaction and/or third party fees"))
			{
				return PartialActivityType.Fee;
			}

			if (Description.Equals("Dividend Tax"))
			{
				return PartialActivityType.Tax;
			}

			if (Description.Contains("Sell"))
			{
				return PartialActivityType.Sell;
			}

			if (Description.Contains("Buy"))
			{
				return PartialActivityType.Buy;
			}

			if (Description.Equals("Dividend"))
			{
				return PartialActivityType.Dividend;
			}
			
			return null;
		}

		public override decimal GetQuantity()
		{
			var quantity = Regex.Match(Description!, "[Buy|Sell] (?<amount>\\d+) (.*)@(?<price>[0-9]+[.0-9]+) (?<currency>[A-Z]+)").Groups[2].Value;

			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		public override decimal GetUnitPrice()
		{
			var quantity = Regex.Match(Description!, "[Buy|Sell] (?<amount>\\d+) (.*)@(?<price>[0-9]+[.0-9]+) (?<currency>[A-Z]+)").Groups[3].Value;

			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		public override Currency GetCurrency(ICurrencyMapper currencyMapper)
		{
			var currency = Regex.Match(Description!, "[Buy|Sell] (?<amount>\\d+) (.*)@(?<price>[0-9]+[.0-9]+) (?<currency>[A-Z]+)").Groups[4].Value;

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
				activity = PartialActivityType.Dividend;
			}

			TransactionId = $"{activity}_{recordDate.ToInvariantString()}_{Product}_{ISIN}_{mutation}";
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
	}
}