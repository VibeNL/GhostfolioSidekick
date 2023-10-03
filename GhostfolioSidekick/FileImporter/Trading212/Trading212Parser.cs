using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Trading212
{
	public class Trading212Parser : RecordBaseImporter<Trading212Record>
	{
		public Trading212Parser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override async Task<IEnumerable<Activity>> ConvertOrders(Trading212Record record, Account account, IEnumerable<Trading212Record> allRecords)
		{
			var orderType = GetOrderType(record);
			if (orderType == null)
			{
				return Array.Empty<Activity>();
			}

			var asset = await api.FindSymbolByISIN(record.ISIN);

			if (string.IsNullOrWhiteSpace(record.Id))
			{
				record.Id = $"{orderType}_{record.ISIN}_{record.Time.ToString("yyyy-MM-dd")}";
			}

			var fee = GetFee(record);

			var order = new Activity
			{
				AccountId = account.Id,
				Asset = asset,
				Currency = record.Currency,
				Date = record.Time,
				Comment = $"Transaction Reference: [{record.Id}]",
				Fee = fee.Fee ?? 0,
				FeeCurrency = fee.Currency,
				Quantity = record.NumberOfShares.Value,
				Type = orderType.Value,
				UnitPrice = record.Price.Value,
				ReferenceCode = record.Id,
			};

			return new[] { order };
		}

		protected override CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ",",
			};
		}

		private (string Currency, decimal? Fee) GetFee(Trading212Record record)
		{
			if (record.FeeUK == null)
			{
				return (record.ConversionFeeCurrency, record.ConversionFee);
			}

			if (record.ConversionFee == null)
			{
				return (record.FeeUKCurrency, record.FeeUK);
			}

			if (record.FeeUK > 0 && record.FeeUKCurrency != record.ConversionFeeCurrency)
			{
				var rate = api.GetExchangeRate(record.FeeUKCurrency, record.ConversionFeeCurrency, record.Time).Result;
				record.FeeUK = record.FeeUK * rate;
			}

			return (record.ConversionFeeCurrency, record.ConversionFee + record.FeeUK);
		}

		private ActivityType? GetOrderType(Trading212Record record)
		{
			return record.Action switch
			{
				"Deposit" or "Interest on cash" or "Currency conversion" => null,
				"Market buy" => ActivityType.BUY,
				"Market sell" => ActivityType.SELL,
				string d when d.Contains("Dividend") => ActivityType.DIVIDEND,
				_ => throw new NotSupportedException(),
			};
		}
	}
}
