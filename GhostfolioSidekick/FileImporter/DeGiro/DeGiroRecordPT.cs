using CsvHelper.Configuration.Attributes;
using GhostfolioSidekick.Model;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.FileImporter.DeGiro
{
	[Delimiter(",")]
	public class DeGiroRecordPT : DeGiroRecordBase
	{
		[Format("dd-MM-yyyy")]
		[Name("Data")]
		public override DateOnly Date { get; set; }

		[Name("Hora")]
		public override TimeOnly Time { get; set; }

		[Name("Data Valor")]
		[Format("dd-MM-yyyy")]
		public override DateOnly CurrencyDate { get; set; }

		[Name("Produto")]
		public override string Product { get; set; }

		[Name("ISIN")]
		public override string ISIN { get; set; }

		[Name("Descrição")]
		public override string Description { get; set; }

		[Name("T.")]
		public override string FX { get; set; }

		[Name("Mudança")]
		public override string Mutation { get; set; }
		
		[Index(8)]
		public override decimal? Total { get; set; }

		[Name("Saldo")]
		public override string BalanceCurrency { get; set; }

		[Index(10)]
		public override decimal Balance { get; set; }

		[Name("ID da Ordem")]
		public override string TransactionId { get; set; }

		public override ActivityType? GetActivityType()
		{
			if (Description.Contains("Venda"))
			{
				return ActivityType.Sell;
			}

			if (Description.Contains("Compra"))
			{
				return ActivityType.Buy;
			}

			if (Description.Equals("Dividendo"))
			{
				return ActivityType.Dividend;
			}

			if (Description.Equals("Processed Flatex Withdrawal"))
			{
				return ActivityType.CashWithdrawal;
			}

			if (Description.Contains("Depósitos"))
			{
				return ActivityType.CashDeposit;
			}

			// TODO, implement other options
			return null;
		}

		public override decimal GetQuantity()
		{
			var quantity = Regex.Match(Description, $"[Venda|Compra] (?<amount>\\d+) @(?<price>[0-9]+,[0-9]+)").Groups[1].Value;

			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		public override decimal GetUnitPrice()
		{
			var quantity = Regex.Match(Description, $"[Venda|Compra] (?<amount>\\d+) @(?<price>[0-9]+,[0-9]+)").Groups[2].Value;

			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		public override bool IsFee()
		{
			return Description == "Comissões de transação DEGIRO e/ou taxas de terceiros";
		}

		public override bool IsTaxes()
		{
			return false; // Not implemented
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
