using CsvHelper;
using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
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

		public Task<bool> CanConvertOrders(IEnumerable<string> filenames)
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


		public async Task<IEnumerable<Order>> ConvertToOrders(string accountName, IEnumerable<string> filenames)
		{
			var list = new ConcurrentDictionary<Tuple<string, Asset, string, DateTime, decimal, decimal>, Order>();
			Tuple<string, Asset, string, DateTime, decimal, decimal> GetKey(Order x)
			{
				return Tuple.Create(x.AccountId, x.Asset, x.Currency, x.Date, x.UnitPrice, x.Quantity);
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

			return list.Values;
		}

		private async Task<Order?> ConvertToOrder(Account account, BaaderBankRKKRecord record)
		{
			var orderType = GetOrderType(record);
			if (orderType == null)
			{
				return null;
			}

			var asset = await api.FindSymbolByISIN(record.Isin.Replace("ISIN ", string.Empty));

			var quantity = decimal.Parse(record.Quantity.Replace("STK ", string.Empty), GetCultureForParsingNumbers());
			var unitPrice = record.UnitPrice.GetValueOrDefault() / quantity;
			return new Order
			{
				AccountId = account.Id,
				Asset = asset,
				Comment = $"Transaction Reference: [{record.Reference}]",
				Currency = record.Currency,
				Date = record.Date.ToDateTime(TimeOnly.MinValue),
				Fee = 0,
				FeeCurrency = record.Currency,
				Quantity = quantity,
				ReferenceCode = record.Reference,
				Type = orderType.Value,
				UnitPrice = unitPrice
			};
		}

		private async Task<Order> ConvertToOrder(Account account, BaaderBankWUMRecord record, ConcurrentDictionary<string, BaaderBankRKKRecord> rkkRecords)
		{
			var asset = await api.FindSymbolByISIN(record.Isin);

			var fee = FindFeeRecord(rkkRecords, record.Reference);

			return new Order
			{
				AccountId = account.Id,
				Asset = asset,
				Comment = $"Transaction Reference: [{record.Reference}]",
				Currency = record.Currency,
				Date = record.Date.ToDateTime(record.Time),
				Fee = Math.Abs(fee?.UnitPrice ?? 0),
				FeeCurrency = fee?.Currency ?? record.Currency,
				Quantity = Math.Abs(record.Quantity.GetValueOrDefault()),
				ReferenceCode = record.Reference,
				Type = GetOrderType(record),
				UnitPrice = record.UnitPrice.GetValueOrDefault()
			};
		}

		private BaaderBankRKKRecord? FindFeeRecord(ConcurrentDictionary<string, BaaderBankRKKRecord> rkkRecords, string reference)
		{
			if (rkkRecords.TryGetValue(reference, out var baaderBankRKKRecord))
			{
				return baaderBankRKKRecord;
			}

			return null;
		}

		private OrderType GetOrderType(BaaderBankWUMRecord record)
		{
			switch (record.OrderType)
			{
				case "Verkauf":
					return OrderType.SELL;
				case "Kauf":
					return OrderType.BUY;
				default:
					throw new NotSupportedException();
			}
		}

		private OrderType? GetOrderType(BaaderBankRKKRecord record)
		{
			if (record.OrderType == "Coupons/Dividende")
			{
				return OrderType.DIVIDEND;
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
