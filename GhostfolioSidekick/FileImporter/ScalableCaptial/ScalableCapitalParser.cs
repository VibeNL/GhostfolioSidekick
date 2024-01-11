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

		public async Task<IEnumerable<Activity>> ConvertToActivities(string fileName, Balance accountBalance)
		{
			var list = new ConcurrentDictionary<Tuple<SymbolProfile?, Currency, DateTime, decimal, decimal>, Activity>();
			Tuple<SymbolProfile?, Currency, DateTime, decimal, decimal> GetKey(Activity x)
			{
				return Tuple.Create(x.Asset, x.UnitPrice.Currency, x.Date, x.UnitPrice.Amount, x.Quantity);
			};

			var wumRecords = new ConcurrentBag<BaaderBankWUMRecord>();
			var rkkRecords = new ConcurrentDictionary<string, BaaderBankRKKRecord>();

			CsvConfiguration csvConfig = GetConfig();

			using var streamReader = File.OpenText(fileName);
			using var csvReader = new CsvReader(streamReader, csvConfig);

			csvReader.Read();
			csvReader.ReadHeader();

			if (IsWUMRecord(csvReader))
			{
				csvReader.GetRecords<BaaderBankWUMRecord>().ToList().ForEach(x => wumRecords.Add(x));
			}

			if (IsRKKRecord(csvReader))
			{
				csvReader.GetRecords<BaaderBankRKKRecord>().ToList().ForEach(x =>
				{
					rkkRecords.TryAdd(DetermineKey(x), x);

					static string DetermineKey(BaaderBankRKKRecord x)
					{
						return x.Reference == "-" ? Guid.NewGuid().ToString() : x.Reference;
					}
				});
			}

			foreach (var record in wumRecords)
			{
				var order = await ConvertToOrder(CurrencyHelper.ParseCurrency(record.Currency) ?? accountBalance.Currency, record, rkkRecords);
				if (order != null)
				{
					list.TryAdd(GetKey(order), order);
				}
			};

			foreach (var record in rkkRecords)
			{
				BaaderBankRKKRecord r = record.Value;
				var order = await ConvertToOrder(CurrencyHelper.ParseCurrency(record.Value.Currency) ?? accountBalance.Currency, r);
				if (order != null)
				{
					list.TryAdd(GetKey(order), order);
				}

				if (r.OrderType == "Saldo")
				{
					order = Activity.GetKnownBalance(new Money(r.Currency, r.UnitPrice.GetValueOrDefault(0), r.Date.ToDateTime(TimeOnly.MinValue)));
					list.TryAdd(GetKey(order), order);
				}
			};

			return list.Values;
		}

		private async Task<Activity?> ConvertToOrder(Currency expectedCurrency, BaaderBankRKKRecord record)
		{
			var orderType = GetOrderType(record);
			if (orderType == null)
			{
				return null;
			}

			var asset = await api.FindSymbolByIdentifier(
				record.Isin.Replace("ISIN ", string.Empty),
				expectedCurrency,
				DefaultSetsOfAssetClasses.StockBrokerDefaultSetAssestClasses,
				DefaultSetsOfAssetClasses.StockBrokerDefaultSetAssetSubClasses);

			var quantity = decimal.Parse(record.Quantity.Replace("STK ", string.Empty), GetCultureForParsingNumbers());
			var unitPrice = record.UnitPrice.GetValueOrDefault() / quantity;

			return new Activity(
				orderType.Value,
				asset,
				record.Date.ToDateTime(TimeOnly.MinValue),
				quantity,
				new Money(record.Currency, unitPrice, record.Date.ToDateTime(TimeOnly.MinValue)),
				null,
				TransactionReferenceUtilities.GetComment(record.Reference, record.Isin),
				record.Reference
				);
		}

		private async Task<Activity> ConvertToOrder(Currency expectedCurrency, BaaderBankWUMRecord record, ConcurrentDictionary<string, BaaderBankRKKRecord> rkkRecords)
		{
			var asset = await api.FindSymbolByIdentifier(
				record.Isin,
				expectedCurrency,
				DefaultSetsOfAssetClasses.StockBrokerDefaultSetAssestClasses,
				DefaultSetsOfAssetClasses.StockBrokerDefaultSetAssetSubClasses);

			var fees = GetFees(rkkRecords, record.Reference, record.Date.ToDateTime(record.Time));

			return new Activity(
				GetOrderType(record),
				asset,
				record.Date.ToDateTime(record.Time),
				Math.Abs(record.Quantity.GetValueOrDefault()),
				new Money(record.Currency, record.UnitPrice.GetValueOrDefault(), record.Date.ToDateTime(record.Time)),
				fees,
				TransactionReferenceUtilities.GetComment(record.Reference, record.Isin),
				record.Reference
				);
		}

		private IEnumerable<Money> GetFees(ConcurrentDictionary<string, BaaderBankRKKRecord> rkkRecords, string reference, DateTime dateTime)
		{
			if (rkkRecords.TryGetValue(reference, out var record) && record != null)
			{
				return new[] { new Money(record.Currency, Math.Abs(record?.UnitPrice ?? 0), dateTime) };
			}

			return Enumerable.Empty<Money>();
		}

		private ActivityType GetOrderType(BaaderBankWUMRecord record)
		{
			switch (record.OrderType)
			{
				case "Verkauf":
					return ActivityType.Sell;
				case "Kauf":
					return ActivityType.Buy;
				default:
					throw new NotSupportedException();
			}
		}

		private ActivityType? GetOrderType(BaaderBankRKKRecord record)
		{
			if (record.OrderType == "Coupons/Dividende")
			{
				return ActivityType.Dividend;
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

		public Task<bool> CanParseActivities(string fileName)
		{
			throw new NotImplementedException();
		}

	}
}
