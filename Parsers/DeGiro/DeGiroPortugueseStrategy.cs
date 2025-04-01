using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	public class DeGiroPortugueseStrategy : IDeGiroStrategy
	{
		public PartialActivityType? GetActivityType(DeGiroRecord record)
		{
			if (string.IsNullOrWhiteSpace(record.Description))
			{
				return null;
			}

			if (record.Description == "Comissões de transação DEGIRO e/ou taxas de terceiros")
			{
				return PartialActivityType.Fee;
			}

			if (record.Description.Contains("Venda"))
			{
				return PartialActivityType.Sell;
			}

			if (record.Description.Contains("Compra"))
			{
				return PartialActivityType.Buy;
			}

			if (record.Description.Equals("Dividendo"))
			{
				return PartialActivityType.Dividend;
			}

			if (record.Description.Equals("Processed Flatex Withdrawal"))
			{
				return PartialActivityType.CashWithdrawal;
			}

			if (record.Description.Contains("Depósitos"))
			{
				return PartialActivityType.CashDeposit;
			}

			if (record.Description.Contains("Flatex Interest Income"))
			{
				return PartialActivityType.Interest;
			}

			if (record.Description.Contains("Custo de Conectividade DEGIRO"))
			{
				return PartialActivityType.Fee;
			}

			return null;
		}

		public decimal GetQuantity(DeGiroRecord record)
		{
			var quantity = Regex.Match(record.Description!, "[Venda|Compra] (?<amount>\\d+) (.*)@(?<price>[0-9]+[,0-9]+) (?<currency>[A-Z]+)", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Groups[2].Value;

			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		public decimal GetUnitPrice(DeGiroRecord record)
		{
			var quantity = Regex.Match(record.Description!, "[Venda|Compra] (?<amount>\\d+) (.*)@(?<price>[0-9]+[,0-9]+) (?<currency>[A-Z]+)", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Groups[3].Value;

			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		public Currency GetCurrency(DeGiroRecord record, ICurrencyMapper currencyMapper)
		{
			var currency = Regex.Match(record.Description!, "[Venda|Compra] (?<amount>\\d+) (.*)@(?<price>[0-9]+[,0-9]+) (?<currency>[A-Z]+)", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Groups[4].Value;

			return currencyMapper.Map(currency);
		}

		public void SetGenerateTransactionIdIfEmpty(DeGiroRecord record, DateTime recordDate)
		{
			if (!string.IsNullOrWhiteSpace(record.TransactionId))
			{
				return;
			}

			var activity = GetActivityType(record);
			var mutation = record.Mutation;

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
