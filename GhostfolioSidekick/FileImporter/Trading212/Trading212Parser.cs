﻿using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
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
			var activityType = GetOrderType(record);
			if (activityType == null)
			{
				return Array.Empty<Activity>();
			}

			var asset = string.IsNullOrWhiteSpace(record.ISIN) ? null : await api.FindSymbolByIdentifier(
				record.ISIN,
				CurrencyHelper.ParseCurrency(record.Currency) ?? account.Balance.Currency,
				DefaultSetsOfAssetClasses.StockBrokerDefaultSetAssestClasses,
				DefaultSetsOfAssetClasses.StockBrokerDefaultSetAssetSubClasses);

			if (string.IsNullOrWhiteSpace(record.Id))
			{
				record.Id = $"{activityType}_{record.ISIN}_{record.Time.ToString("yyyy-MM-dd")}";
			}

			var fee = GetFee(record);

			if (activityType == ActivityType.Convert)
			{
				var parsed = ParserConvertion(record);
				var activitySource = new Activity(
					ActivityType.CashWithdrawal,
					asset,
					record.Time,
					1,
					parsed.Source,
					null,
					$"Transaction Reference: [{record.Id}_SourceCurrencyConversion]",
					record.Id + "_SourceCurrencyConversion"
					);
				var activityTarget = new Activity(
					ActivityType.CashDeposit,
					asset,
					record.Time,
					1,
					parsed.Target,
					null,
					$"Transaction Reference: [{record.Id}_TargetCurrencyConversion]",
					record.Id + "_TargetCurrencyConversion"
				);

				return new[] { activitySource, activityTarget };
			}
			else if (activityType == ActivityType.CashDeposit ||
				activityType == ActivityType.CashWithdrawal ||
				activityType == ActivityType.Interest)
			{
				var activity = new Activity(
					activityType.Value,
					asset,
					record.Time,
					1,
					new Money(record.Currency == string.Empty ? record.CurrencyTotal : record.Currency, record.Total.GetValueOrDefault(0), record.Time),
					fee.Fee == null ? null : new Money(fee.Currency, fee.Fee ?? 0, record.Time),
					TransactionReferenceUtilities.GetComment(record.Id),
					record.Id
					);
				return new[] { activity };
			}
			else
			{
				var activity = new Activity(
					activityType.Value,
					asset,
					record.Time,
					record.NumberOfShares.Value,
					new Money(record.Currency, record.Price.Value, record.Time),
					fee.Fee == null ? null : new Money(fee.Currency, fee.Fee ?? 0, record.Time),
					TransactionReferenceUtilities.GetComment(record.Id, record.ISIN),
					record.Id
					);
				return new[] { activity };
			}
		}

		private (Money Source, Money Target) ParserConvertion(Trading212Record record)
		{
			// "0.01 GBP -> 0.01 EUR"
			var note = record.Notes;
			var splitted = note.Split(' ');

			Money source = new Money(splitted[1], Decimal.Parse(splitted[0], CultureInfo.InvariantCulture), record.Time);
			Money target = new Money(splitted[4], Decimal.Parse(splitted[3], CultureInfo.InvariantCulture), record.Time);

			return (source, target);
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
			(string Currency, decimal? Fee)? taxes = null;
			if (record.FeeUK != null)
			{
				taxes = (record.FeeUKCurrency, record.FeeUK);
			}
			else if (record.FeeFrance != null)
			{
				taxes = (record.FeeFranceCurrency, record.FeeFrance);
			}

			if (taxes == null)
			{
				return (record.ConversionFeeCurrency, record.ConversionFee);
			}
			
			if (record.ConversionFee == null)
			{
				return taxes.Value;
			}

			var t = taxes.Value;
			if (t.Fee > 0 && t.Currency != record.ConversionFeeCurrency)
			{
				taxes = (record.ConversionFeeCurrency, api.GetConvertedPrice(new Money(t.Currency,t.Fee ?? 0, record.Time), CurrencyHelper.ParseCurrency(record.ConversionFeeCurrency), record.Time).Result.Amount);
			}

			return (record.ConversionFeeCurrency, record.ConversionFee + taxes.Value.Fee);
		}

		private ActivityType? GetOrderType(Trading212Record record)
		{
			return record.Action switch
			{
				"Deposit" => ActivityType.CashDeposit,
   "Withdrawal" => ActivityType.CashWithdrawal,
				"Interest on cash" => ActivityType.Interest,
				"Currency conversion" => ActivityType.Convert,
				"Market buy" => ActivityType.Buy,
				"Market sell" => ActivityType.Sell,
				string d when d.Contains("Dividend") => ActivityType.Dividend,
				_ => throw new NotSupportedException(),
			};
		}
	}
}
