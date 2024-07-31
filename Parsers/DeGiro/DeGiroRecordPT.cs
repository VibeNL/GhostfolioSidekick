using CsvHelper.Configuration.Attributes;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	[Delimiter(",")]
	public class DeGiroRecordPT : DeGiroRecordBase
	{
		[Format("dd-MM-yyyy")]
		[Name("Data")]
		public override DateOnly Date { get; set; }

		[Name("Hora")]
		public override TimeOnly Time { get; set; }

		[ExcludeFromCodeCoverage]
		[Name("Data Valor")]
		[Format("dd-MM-yyyy")]
		public override DateOnly CurrencyDate { get; set; }

		[Name("Produto")]
		public override string? Product { get; set; }

		[Name("ISIN")]
		public override string? ISIN { get; set; }

		[Name("Descrição")]
		public override required string Description { get; set; }

		[ExcludeFromCodeCoverage]
		[Name("T.")]
		public override string? FX { get; set; }

		[Name("Mudança")]
		public override required string Mutation { get; set; }

		[Index(8)]
		public override decimal? Total { get; set; }

		[Name("Saldo")]
		public override required string BalanceCurrency { get; set; }

		[Index(10)]
		public override decimal Balance { get; set; }

		[Name("ID da Ordem")]
		public override string? TransactionId { get; set; }

		public override PartialActivityType? GetActivityType()
		{
			if (string.IsNullOrWhiteSpace(Description))
			{
				return null;
			}

			if (Description == "Comissões de transação DEGIRO e/ou taxas de terceiros")
			{
				return PartialActivityType.Fee;
			}

			if (Description.Contains("Venda"))
			{
				return PartialActivityType.Sell;
			}

			if (Description.Contains("Compra"))
			{
				return PartialActivityType.Buy;
			}

			if (Description.Equals("Dividendo"))
			{
				return PartialActivityType.Dividend;
			}

			if (Description.Equals("Processed Flatex Withdrawal"))
			{
				return PartialActivityType.CashWithdrawal;
			}

			if (Description.Contains("Depósitos"))
			{
				return PartialActivityType.CashDeposit;
			}

			if (Description.Contains("Flatex Interest Income"))
			{
				return PartialActivityType.Interest;
			}

			if (Description.Contains("Custo de Conectividade DEGIRO"))
			{
				return PartialActivityType.Fee;
			}

			return null;
		}

		public override decimal GetQuantity()
		{
			var quantity = Regex.Match(Description!, "[Venda|Compra] (?<amount>\\d+) (.*)@(?<price>[0-9]+[,0-9]+) (?<currency>[A-Z]+)", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Groups[2].Value;

			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		public override decimal GetUnitPrice()
		{
			var quantity = Regex.Match(Description!, "[Venda|Compra] (?<amount>\\d+) (.*)@(?<price>[0-9]+[,0-9]+) (?<currency>[A-Z]+)", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Groups[3].Value;

			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		public override Currency GetCurrency(ICurrencyMapper currencyMapper)
		{
			var currency = Regex.Match(Description!, "[Venda|Compra] (?<amount>\\d+) (.*)@(?<price>[0-9]+[,0-9]+) (?<currency>[A-Z]+)", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Groups[4].Value;

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

			TransactionId = $"{activity}_{recordDate.ToInvariantString()}_{Product}_{ISIN}_{mutation}";
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
