using CsvHelper;
using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	public class DeGiroParser : IActivityFileImporter
	{
		private readonly Dictionary<string, bool> KnownHeaderCache = [];
		private readonly ICurrencyMapper currencyMapper;

		private static Type[] typeOfRecords = new[]{
			typeof(DeGiroRecordEN),
			typeof(DeGiroRecordNL),
			typeof(DeGiroRecordPT),
		};

		public DeGiroParser(ICurrencyMapper currencyMapper)
		{
			this.currencyMapper = currencyMapper;
		}

		public virtual Task<bool> CanParse(string filename)
		{
			if (!filename.EndsWith(".csv", StringComparison.InvariantCultureIgnoreCase))
			{
				return Task.FromResult(false);
			}

			return Task.FromResult(GetTypeOfRecord(filename));
		}

		public Task ParseActivities(string filename, IActivityManager activityManager, string accountName)
		{
			var dictionary = new Dictionary<Type, List<PartialActivity>>();
			foreach (var typeOfRecord in typeOfRecords)
			{
				dictionary.Add(typeOfRecord, new List<PartialActivity>());

				try
				{
					var csvConfig = GetConfig();
					using var streamReader = GetStreamReader(filename);
					using var csvReader = new CsvReader(streamReader, csvConfig);
					csvReader.Read();
					csvReader.ReadHeader();
					var records = csvReader.GetRecords(typeOfRecord).ToList();

					for (int i = 0; i < records.Count; i++)
					{
						var partialActivity = ParseRow((DeGiroRecordBase)records[i], i + 1);
						dictionary[typeOfRecord].AddRange(partialActivity.Where(x => x != null));
					}
				}
				catch
				{
					continue;
				}
			}

			// get most activities
			var mostActivities = dictionary.OrderByDescending(x => x.Value.Count).First().Value;

			// add activities to activity manager
			activityManager.AddPartialActivity(accountName, mostActivities);

			return Task.CompletedTask;
		}

		protected virtual StreamReader GetStreamReader(string file)
		{
			return File.OpenText(file);
		}

		private IEnumerable<PartialActivity> ParseRow(DeGiroRecordBase record, int rowNumber)
		{
			var recordDate = DateTime.SpecifyKind(record.Date.ToDateTime(record.Time), DateTimeKind.Utc);

			var knownBalance = PartialActivity.CreateKnownBalance(
				currencyMapper.Map(record.BalanceCurrency),
				recordDate,
				record.Balance,
				rowNumber);
			PartialActivity? partialActivity;

			var activityType = record.GetActivityType();

			var currencyRecord = !string.IsNullOrWhiteSpace(record.Mutation) ? currencyMapper.Map(record.Mutation) : record.GetCurrency(currencyMapper);
			var recordTotal = Math.Abs(record.Total.GetValueOrDefault());

			record.SetGenerateTransactionIdIfEmpty(recordDate);

			switch (activityType)
			{
				case null:
				case PartialActivityType.Undefined:
					return [knownBalance];
				case PartialActivityType.Buy:
					partialActivity = PartialActivity.CreateBuy(
						record.GetCurrency(currencyMapper),
						recordDate,
						[PartialSymbolIdentifier.CreateStockAndETF(record.ISIN!)],
						record.GetQuantity(),
						record.GetUnitPrice(),
						new Money(currencyRecord, GetRecordTotal(recordTotal, record.GetQuantity(), record.GetUnitPrice())),
						record.TransactionId!);
					break;
				case PartialActivityType.CashDeposit:
					partialActivity = PartialActivity.CreateCashDeposit(currencyRecord, recordDate, recordTotal, new Money(currencyRecord, recordTotal), record.TransactionId!);
					break;
				case PartialActivityType.CashWithdrawal:
					partialActivity = PartialActivity.CreateCashWithdrawal(currencyRecord, recordDate, recordTotal, new Money(currencyRecord, recordTotal), record.TransactionId!);
					break;
				case PartialActivityType.Dividend:
					partialActivity = PartialActivity.CreateDividend(currencyRecord, recordDate,
						[PartialSymbolIdentifier.CreateStockAndETF(record.ISIN!)], recordTotal, new Money(currencyRecord, recordTotal), record.TransactionId!);
					break;
				case PartialActivityType.Fee:
					partialActivity = PartialActivity.CreateFee(currencyRecord, recordDate, recordTotal, new Money(currencyRecord, recordTotal), record.TransactionId!);
					break;
				case PartialActivityType.Tax:
					partialActivity = PartialActivity.CreateTax(currencyRecord, recordDate, recordTotal, new Money(currencyRecord, recordTotal), record.TransactionId!);
					break;
				case PartialActivityType.Interest:
					partialActivity = PartialActivity.CreateInterest(currencyRecord, recordDate, recordTotal, record.Description, new Money(currencyRecord, recordTotal), record.TransactionId!);
					break;
				case PartialActivityType.Sell:
					partialActivity = PartialActivity.CreateSell(
						record.GetCurrency(currencyMapper),
						recordDate,
						[PartialSymbolIdentifier.CreateStockAndETF(record.ISIN!)],
						record.GetQuantity(),
						record.GetUnitPrice(),
						new Money(currencyRecord, GetRecordTotal(recordTotal, record.GetQuantity(), record.GetUnitPrice())),
						record.TransactionId!);
					break;
				default:
					throw new NotSupportedException();
			}

			return [knownBalance, partialActivity];
		}

		private static decimal GetRecordTotal(decimal recordTotal, decimal quantity, decimal unitPrice)
		{
			if (recordTotal == 0)
			{
				recordTotal = Math.Abs(quantity * unitPrice);
			}

			return recordTotal;
		}

		private bool GetTypeOfRecord(string filename)
		{
			CsvConfiguration csvConfig = GetConfig();

			using var streamReader = GetStreamReader(filename);
			using var csvReader = new CsvReader(streamReader, csvConfig);
			csvReader.Read();
			csvReader.ReadHeader();

			string? record = string.Join("|", csvReader.HeaderRecord!);
			if (KnownHeaderCache.TryGetValue(record, out var canParse))
			{
				return canParse;
			}

			foreach (var typeOfRecord in typeOfRecords)
			{
				try
				{
					csvReader.ValidateHeader(typeOfRecord);
					KnownHeaderCache.Add(record, true);
					return true;
				}
				catch
				{
					continue;
				}
			}

			return false;
		}

		protected CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ",",

			};
		}
	}
}