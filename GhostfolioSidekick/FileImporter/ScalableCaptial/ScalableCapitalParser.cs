using CsvHelper.Configuration;
using CsvHelper;
using GhostfolioSidekick.Ghostfolio.API;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace GhostfolioSidekick.FileImporter.ScalableCaptial
{

	public class ScalableCapitalParser : IFileImporter
	{
		private IEnumerable<IFileImporter> fileImporters;

		private IGhostfolioAPI api;

		public ScalableCapitalParser(IGhostfolioAPI api)
		{
			this.api = api;
		}

		public async Task<bool> CanConvertOrders(IEnumerable<string> filenames)
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
					return false;
				}
			}

			return true;
		}


		public async Task<IEnumerable<Order>> ConvertToOrders(string accountName, IEnumerable<string> filenames)
		{
			var list = new ConcurrentDictionary<Tuple<string, Asset, string, DateTime, decimal, decimal>, Order>();
			Tuple<string, Asset, string, DateTime, decimal, decimal> GetKey(Order x)
			{
				return Tuple.Create(x.AccountId, x.Asset, x.Currency, x.Date, x.UnitPrice, x.Quantity);
			};


			var wumRecords = new ConcurrentBag<BaaderBankWUMRecord>();
			var rkkRecords = new ConcurrentBag<BaaderBankRKKRecord>();

			var account = await api.GetAccountByName(accountName);

			if (account == null)
			{
				throw new NotSupportedException();
			}

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
					csvReader.GetRecords<BaaderBankRKKRecord>().ToList().ForEach(x => rkkRecords.Add(x));
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
				var order = await ConvertToOrder(account, record);
				if (order != null)
				{
					list.TryAdd(GetKey(order), order);
				}
			});

			return list.Values;
		}

		private async Task<Order> ConvertToOrder(Account account, BaaderBankRKKRecord record)
		{
			var orderType = GetOrderType(record);
			if (orderType == null)
			{
				return null;
			}

			var asset = await api.FindSymbolByISIN(record.Isin.Replace("ISIN ", string.Empty));

			var quantity = decimal.Parse(record.Quantity.Replace("STK ", string.Empty), GetCultureForParsingNumbers());
			var unitPrice = record.UnitPrice.Value / quantity;
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

		private async Task<Order> ConvertToOrder(Account account, BaaderBankWUMRecord record, IEnumerable<BaaderBankRKKRecord> rkkRecords)
		{
			var asset = await api.FindSymbolByISIN(record.Isin);

			var fee = FindFeeRecord(rkkRecords, record.Reference);

			return new Order
			{
				AccountId = account.Id,
				Asset = asset,
				Comment = $"Transaction Reference: [{record.Reference}]",
				Currency = record.Currency,
				Date = record.Date.ToDateTime(TimeOnly.MinValue),
				Fee = Math.Abs(fee?.UnitPrice ?? 0),
				FeeCurrency = fee?.Currency ?? record.Currency,
				Quantity = Math.Abs(record.Quantity.Value),
				ReferenceCode = record.Reference,
				Type = GetOrderType(record),
				UnitPrice = record.UnitPrice.Value
			};
		}

		private BaaderBankRKKRecord? FindFeeRecord(IEnumerable<BaaderBankRKKRecord> rkkRecords, string reference)
		{
			return rkkRecords.FirstOrDefault(x => x.Reference == reference);
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
