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

		protected override async Task<IEnumerable<Model.Activity>> ConvertOrders(Trading212Record record, Model.Account account, IEnumerable<Trading212Record> allRecords)
		{
			var activityType = GetOrderType(record);
			if (activityType == null)
			{
				return Array.Empty<Model.Activity>();
			}

			var asset = await api.FindSymbolByISIN(record.ISIN);

			if (string.IsNullOrWhiteSpace(record.Id))
			{
				record.Id = $"{activityType}_{record.ISIN}_{record.Time.ToString("yyyy-MM-dd")}";
			}

			var fee = GetFee(record);
			Model.Activity activity;
			if (activityType == Model.ActivityType.CashDeposit ||
				activityType == Model.ActivityType.CashWithdrawal ||
				activityType == Model.ActivityType.Interest)
			{
				activity = new Model.Activity(
					activityType.Value,
					asset,
					record.Time,
					1,
					new Model.Money(record.Currency, record.Total.GetValueOrDefault(0), record.Time),
					fee.Fee == null ? null : new Model.Money(fee.Currency, fee.Fee ?? 0, record.Time),
					$"Transaction Reference: [{record.Id}]",
					record.Id
					);
			}
			else
			{
				activity = new Model.Activity(
					activityType.Value,
					asset,
					record.Time,
					record.NumberOfShares.Value,
					new Model.Money(record.Currency, record.Price.Value, record.Time),
					fee.Fee == null ? null : new Model.Money(fee.Currency, fee.Fee ?? 0, record.Time),
					$"Transaction Reference: [{record.Id}]",
					record.Id
					);
			}

			return new[] { activity };
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
				if (record.FeeUK > 0)
				{
					record.FeeUK = api.GetConvertedPrice(new Model.Money(record.FeeUKCurrency, record.FeeUK ?? 0, record.Time), CurrencyHelper.ParseCurrency(record.ConversionFeeCurrency), record.Time).Result.Amount;
				}
			}

			return (record.ConversionFeeCurrency, record.ConversionFee + record.FeeUK);
		}

		private Model.ActivityType? GetOrderType(Trading212Record record)
		{
			return record.Action switch
			{
				"Deposit" => Model.ActivityType.CashDeposit,
				"Interest on cash" => Model.ActivityType.Interest,
				"Currency conversion" => Model.ActivityType.Convert,
				"Market buy" => Model.ActivityType.Buy,
				"Market sell" => Model.ActivityType.Sell,
				string d when d.Contains("Dividend") => Model.ActivityType.Dividend,
				_ => throw new NotSupportedException(),
			};
		}
	}
}
