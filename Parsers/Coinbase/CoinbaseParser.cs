using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Parsers.Coinbase
{
	public partial class CoinbaseParser(ICurrencyMapper currencyMapper) : RecordBaseImporter<CoinbaseRecord>
	{
		protected override IEnumerable<PartialActivity> ParseRow(CoinbaseRecord record, int rowNumber)
		{
			var date = record.Timestamp;

			var id = record.TransactionId ?? $"{record.Type}_{record.Asset}_{date.ToInvariantString()}";

			var currency = currencyMapper.Map(record.Currency);
			if (record.Fee != null && record.Fee > 0) // Negative fees are not supported
			{
				yield return PartialActivity.CreateFee(currency, date, record.Fee ?? 0, new Money(currency, 0), id);
			}

			switch (record.Type)
			{
				case string when record.Type.Equals("Retail Eth2 Deprecation") && record.Quantity > 0:
				case string when record.Type.Contains("Buy", StringComparison.InvariantCultureIgnoreCase):

					if (Currency.GetCurrency(record.Asset).IsFiat())
					{
						yield break;
					}

					yield return PartialActivity.CreateBuy(
						currency,
						date,
						[PartialSymbolIdentifier.CreateCrypto(record.Asset)],
						record.Quantity,
						new Money(currency, record.Price!.Value),
						new Money(currency, record.TotalTransactionAmount!.Value),
						id);
					break;
				case string when record.Type.Equals("Retail Eth2 Deprecation") && record.Quantity < 0:
				case string when record.Type.Contains("Sell", StringComparison.InvariantCultureIgnoreCase):
					yield return PartialActivity.CreateSell(
						currency,
						date,
						[PartialSymbolIdentifier.CreateCrypto(record.Asset)],
						record.Quantity,
						new Money(currency, record.Price!.Value),
						new Money(currency, record.TotalTransactionAmount!.Value),
						id);
					break;
				case "Deposit":
					yield return PartialActivity.CreateCashDeposit(
						currency,
						date,
						record.Quantity,
						new Money(currency, record.TotalTransactionAmount!.Value),
						id);
					break;
				case "Withdrawal":
					yield return PartialActivity.CreateCashWithdrawal(
						currency,
						date,
						record.Quantity,
						new Money(currency, record.TotalTransactionAmount!.Value),
						id);
					break;
				case "Receive":
					yield return PartialActivity.CreateReceive(date, [PartialSymbolIdentifier.CreateCrypto(record.Asset)], record.Quantity, id);
					break;
				case "Send":
					yield return PartialActivity.CreateSend(date, [PartialSymbolIdentifier.CreateCrypto(record.Asset)], record.Quantity, id);
					break;
				case "Convert":
					var result = ParseNote(record.Notes);
					var parseAmount = result.Item1;
					string parsedAsset = result.Item2;

					var lst = PartialActivity.CreateAssetConvert(
						date,
						[PartialSymbolIdentifier.CreateCrypto(record.Asset)],
						record.Quantity,
						[PartialSymbolIdentifier.CreateCrypto(parsedAsset)],
						parseAmount,
						id);

					foreach (var item in lst)
					{
						yield return item;
					}
					break;
				case "Staking Income":
				case "Rewards Income":
					yield return PartialActivity.CreateStakingReward(date, [PartialSymbolIdentifier.CreateCrypto(record.Asset)], record.Quantity, id);
					break;
				case "Learning Reward":
					yield return PartialActivity.CreateGift(date, [PartialSymbolIdentifier.CreateCrypto(record.Asset)], record.Quantity, id);
					break;
				default:
					throw new NotSupportedException(record.Type);
			}
		}

		private static (decimal, string) ParseNote(string note)
		{
			// Converted 0.00087766 ETH to 1.629352 USDC or  Converted 0,00087766 ETH to 1,629352 USDC
			var match = NoteRegex().Match(note);
			var quantity = match.Groups[3].Value;
			var asset = match.Groups[4].Value;

			var amountEn = decimal.Parse(quantity, GetCultureForParsingNumbersEn());
			var amountNl = decimal.Parse(quantity, GetCultureForParsingNumbersNl());

			return (Math.Min(amountEn, amountNl), asset);
		}

		protected override CsvConfiguration GetConfig()
		{
			bool hasStarted = false;
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ",",
				ShouldSkipRecord = (r) =>
				{
					hasStarted = hasStarted || (r.Row[0]!.StartsWith("Timestamp") || r.Row[0]!.StartsWith("ID"));
					return !hasStarted;
				},
			};
		}

		private static CultureInfo GetCultureForParsingNumbersEn()
		{
			return new CultureInfo("en");
		}

		private static CultureInfo GetCultureForParsingNumbersNl()
		{
			return new CultureInfo("nl");
		}

		[GeneratedRegex("Converted ([0-9.,]+) ([A-Za-z0-9]+) to ([0-9.,]+) ([A-Za-z0-9]+)", RegexOptions.IgnoreCase, "nl-NL")]
		private static partial Regex NoteRegex();
	}
}
