using CsvHelper;
using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using System.Collections.Concurrent;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.ScalableCaptial
{

	public class ScalableCapitalParser : IFileImporter
	{
		private readonly IGhostfolioAPI api;

		public ScalableCapitalParser(IGhostfolioAPI api)
		{
			this.api = api;
		}

		public Task<bool> CanParseActivities(IEnumerable<string> filenames)
		{
			foreach (var file in filenames)
			{
				CsvConfiguration csvConfig = GetConfig();

				using var streamReader = File.OpenText(file);
				using var csvReader = new CsvReader(streamReader, csvConfig);

				csvReader.Read();
				csvReader.ReadHeader();

				var canParse = IsWUMRecord(csvReader) || IsRKKRecord(csvReader);
				if (!canParse)
				{
					return Task.FromResult(false);
				}
			}

			return Task.FromResult(true);
		}


		public async Task<Model.Account> ConvertActivitiesForAccount(string accountName, IEnumerable<string> filenames)
		{
			var list = new ConcurrentDictionary<Tuple<Model.Asset, Currency, DateTime, decimal?, decimal>, Model.Activity>();
			Tuple<Model.Asset, Currency, DateTime, decimal?, decimal> GetKey(Model.Activity x)
			{
				return Tuple.Create(x.Asset, x.UnitPrice.Currency, x.Date, x.UnitPrice.Amount, x.Quantity);
			};


			var wumRecords = new ConcurrentBag<BaaderBankWUMRecord>();
			var rkkRecords = new ConcurrentDictionary<string, BaaderBankRKKRecord>();

			var account = await api.GetAccountByName(accountName) ?? throw new NotSupportedException($"Account not found {accountName}");

			Parallel.ForEach(filenames, filename =>
			{
				CsvConfiguration csvConfig = GetConfig();

				using var streamReader = File.OpenText(filename);
				using var csvReader = new CsvReader(streamReader, csvConfig);

				csvReader.Read();
				csvReader.ReadHeader();

				if (IsWUMRecord(csvReader))
				{
					csvReader.GetRecords<BaaderBankWUMRecord>().ToList().ForEach(x => wumRecords.Add(x));
				}

				if (IsRKKRecord(csvReader))
				{
					csvReader.GetRecords<BaaderBankRKKRecord>().ToList().ForEach(x => rkkRecords.TryAdd(x.Reference, x));
				}
			});

			Parallel.ForEach(wumRecords, async record =>
			{
				var order = await ConvertToOrder(account, record, rkkRecords);
				if (order != null)
				{
					list.TryAdd(GetKey(order), order);
				}
			});

			Parallel.ForEach(rkkRecords, async record =>
			{
				var order = await ConvertToOrder(account, record.Value);
				if (order != null)
				{
					list.TryAdd(GetKey(order), order);
				}
			});

			account.ReplaceActivities(list.Values);

			return account;
		}

		private async Task<Model.Activity?> ConvertToOrder(Model.Account account, BaaderBankRKKRecord record)
		{
			var orderType = GetOrderType(record);
			if (orderType == null)
			{
				return null;
			}

			var asset = await api.FindSymbolByISIN(record.Isin.Replace("ISIN ", string.Empty));

			var quantity = decimal.Parse(record.Quantity.Replace("STK ", string.Empty), GetCultureForParsingNumbers());
			var unitPrice = record.UnitPrice.GetValueOrDefault() / quantity;

			return new Model.Activity(
				orderType.Value,
				asset,
				record.Date.ToDateTime(TimeOnly.MinValue),
				quantity,
				new Money(record.Currency, unitPrice),
				new Money(record.Currency, 0),
				$"Transaction Reference: [{record.Reference}]",
				record.Reference
				);
		}

		private async Task<Model.Activity> ConvertToOrder(Model.Account account, BaaderBankWUMRecord record, ConcurrentDictionary<string, BaaderBankRKKRecord> rkkRecords)
		{
			var asset = await api.FindSymbolByISIN(record.Isin);

			var fee = FindFeeRecord(rkkRecords, record.Reference);

			return new Model.Activity(
				GetOrderType(record),
				asset,
				record.Date.ToDateTime(record.Time),
				Math.Abs(record.Quantity.GetValueOrDefault()),
				new Money(record.Currency, record.UnitPrice.GetValueOrDefault()),
				new Money(fee?.Currency ?? record.Currency, Math.Abs(fee?.UnitPrice ?? 0)),
				$"Transaction Reference: [{record.Reference}]",
				record.Reference
				);
		}

		private BaaderBankRKKRecord? FindFeeRecord(ConcurrentDictionary<string, BaaderBankRKKRecord> rkkRecords, string reference)
		{
			if (rkkRecords.TryGetValue(reference, out var baaderBankRKKRecord))
			{
				return baaderBankRKKRecord;
			}

			return null;
		}

		private Model.ActivityType GetOrderType(BaaderBankWUMRecord record)
		{
			switch (record.OrderType)
			{
				case "Verkauf":
					return Model.ActivityType.Sell;
				case "Kauf":
					return Model.ActivityType.Buy;
				default:
					throw new NotSupportedException();
			}
		}

		private Model.ActivityType? GetOrderType(BaaderBankRKKRecord record)
		{
			if (record.OrderType == "Coupons/Dividende")
			{
				return Model.ActivityType.Dividend;
			}

			return null;
		}

		private CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ";",
			};
		}

		private static bool IsRKKRecord(CsvReader csvReader)
		{
			try
			{
				csvReader.ValidateHeader<BaaderBankRKKRecord>();
				return true;
			}
			catch
			{
				// Ignore
			}

			return false;
		}

		private static bool IsWUMRecord(CsvReader csvReader)
		{
			try
			{
				csvReader.ValidateHeader<BaaderBankWUMRecord>();
				return true;
			}
			catch
			{
				// Ignore
			}

			return false;
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
