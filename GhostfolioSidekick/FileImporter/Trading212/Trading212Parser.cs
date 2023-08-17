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

		protected override async Task<Order?> ConvertOrder(Trading212Record record, Account account, IEnumerable<Trading212Record> allRecords)
		{
			var orderType = GetOrderType(record);
			if (orderType == null)
			{
				return null;
			}

			var asset = await api.FindSymbolByISIN(record.ISIN);

			if (string.IsNullOrWhiteSpace(record.Id))
			{
				record.Id = $"{orderType}_{record.ISIN}_{record.Time.ToString("yyyy-MM-dd")}";
			}

			var order = new Order
			{
				AccountId = account.Id,
				Asset = asset,
				Currency = record.Currency,
				Date = record.Time,
				Comment = $"Transaction Reference: [{record.Id}]",
				Fee = GetFee(record) ?? 0,
				FeeCurrency = record.ConversionFeeCurrency,
				Quantity = record.NumberOfShares.Value,
				Type = orderType.Value,
				UnitPrice = record.Price.Value,
				ReferenceCode = record.Id,
			};

			return order;
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

		private decimal? GetFee(Trading212Record record)
		{
			if (record.FeeUK == null)
			{
				return record.ConversionFee;
			}

			if (record.FeeUK > 0 && record.FeeUKCurrency != record.ConversionFeeCurrency)
			{
				var rate = api.GetExchangeRate(record.FeeUKCurrency, record.ConversionFeeCurrency, record.Time).Result;
				record.FeeUK = record.FeeUK * rate;
			}

			return record.ConversionFee + record.FeeUK;
		}

		private OrderType? GetOrderType(Trading212Record record)
		{
			return record.Action switch
			{
				"Deposit" or "Interest on cash" => null,
				"Market buy" => OrderType.BUY,
				"Market sell" => OrderType.SELL,
				string d when d.Contains("Dividend") => OrderType.DIVIDEND,
				_ => throw new NotSupportedException(),
			};
		}
	}
}
