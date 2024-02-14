﻿using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Parsers.Coinbase
{
	public class CoinbaseParser : RecordBaseImporter<CoinbaseRecord>
	{
		private readonly ICurrencyMapper currencyMapper;

		public CoinbaseParser(ICurrencyMapper currencyMapper)
		{
			this.currencyMapper = currencyMapper;
		}

		protected override IEnumerable<PartialActivity> ParseRow(CoinbaseRecord record, int rowNumber)
		{
			var date = record.Timestamp;

			var id = $"{record.Type}_{record.Asset}_{date.ToInvariantString()}";

			var currency = currencyMapper.Map(record.Currency);
			if (record.Fee != null && record.Fee != 0)
			{
				yield return PartialActivity.CreateFee(currency, date, record.Fee ?? 0, id);
			}

			switch (record.Type)
			{
				case "Buy":
					yield return PartialActivity.CreateBuy(currency, date, [PartialSymbolIdentifier.CreateCrypto(record.Asset)], record.Quantity, record.Price ?? 0, id);
					break;
				case "Sell":
					yield return PartialActivity.CreateSell(currency, date, [PartialSymbolIdentifier.CreateCrypto(record.Asset)], record.Quantity, record.Price ?? 0, id);
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
				case "Rewards Income":
					yield return PartialActivity.CreateStakingReward(date, [PartialSymbolIdentifier.CreateCrypto(record.Asset)], record.Quantity, id);
					break;
				case "Learning Reward":
					yield return PartialActivity.CreateLearningReward(date, [PartialSymbolIdentifier.CreateCrypto(record.Asset)], record.Quantity, id);
					break;
				default:
					throw new NotSupportedException();
			}
		}

		private (decimal, string) ParseNote(string note)
		{
			// Converted 0.00087766 ETH to 1.629352 USDC
			var match = Regex.Match(note, "Converted ([0-9.,]+) ([A-Za-z0-9]+) to ([0-9.,]+) ([A-Za-z0-9]+)", RegexOptions.IgnoreCase);
			var quantity = match.Groups[3].Value;
			var asset = match.Groups[4].Value;

			var amount = decimal.Parse(quantity, GetCultureForParsingNumbers());

			return (amount, asset);
		}

		protected override CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ",",
				ShouldSkipRecord = (r) =>
				{
					return !r.Row[0]!.StartsWith("Timestamp") && !r.Row[0]!.StartsWith("20");
				},
			};
		}

		private static CultureInfo GetCultureForParsingNumbers()
		{
			return new CultureInfo("en");
		}
	}
}
