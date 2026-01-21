using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;
using System.Text;

namespace GhostfolioSidekick.Parsers.CentraalBeheer
{
	public class CentraalBeheerParser : RecordBaseImporter<CentraalBeheerRecord>
	{
		protected override IEnumerable<PartialActivity> ParseRow(CentraalBeheerRecord record, int rowNumber)
		{
			var currency = Currency.EUR; // CentraalBeheer transactions are in EUR

			var transactionId = $"CB-{record.TransactionDate:yyyyMMdd}-{rowNumber}";

			var stockIdentifier = "Centraal Beheer " + record.FundName?.Trim();

			switch (record.TransactionType)
			{
				case "Aankoop": // Purchase
					if (!string.IsNullOrEmpty(record.FundName) && record.NumberOfUnits.HasValue && record.Rate.HasValue)
					{
						var unitPrice = record.Rate.Value;
						var quantity = record.NumberOfUnits.Value;
						var totalAmount = Math.Abs(record.NetAmount.GetValueOrDefault());
						var fee = record.PurchaseCosts ?? 0m;

						yield return PartialActivity.CreateBuy(
							currency,
							record.TransactionDate,
							PartialSymbolIdentifier.CreateStockAndETF(stockIdentifier, GetConstructedId(stockIdentifier)),
							quantity,
							new Money(currency, unitPrice),
							new Money(currency, totalAmount),
							transactionId);

						if (fee > 0)
						{
							yield return PartialActivity.CreateFee(
								currency,
								record.TransactionDate,
								fee,
								new Money(currency, fee),
								transactionId);
						}
					}

					break;

				case "Verkoop":
					if (!string.IsNullOrEmpty(record.FundName) && record.NumberOfUnits.HasValue && record.Rate.HasValue)
					{
						var unitPrice = record.Rate.Value;
						var quantity = Math.Abs(record.NumberOfUnits.Value);
						var totalAmount = Math.Abs(record.NetAmount.GetValueOrDefault());
						var fee = record.PurchaseCosts ?? 0m;

						yield return PartialActivity.CreateSell(
						currency,
						record.TransactionDate,
						PartialSymbolIdentifier.CreateStockAndETF(stockIdentifier, GetConstructedId(stockIdentifier)),
						quantity,
						new Money(currency, unitPrice),
						new Money(currency, totalAmount),
						transactionId);

						if (fee > 0)
						{
							yield return PartialActivity.CreateFee(
								currency,
								record.TransactionDate,
								fee,
								new Money(currency, fee),
								transactionId);
						}
					}
					break;

				case "Overboeking": // Transfer
					var amount = Math.Abs(record.GrossAmount.GetValueOrDefault());
					if (record.DebitCredit == "Bij") // Credit - money coming in
					{
						yield return PartialActivity.CreateCashDeposit(
							currency,
							record.TransactionDate,
							amount,
							new Money(currency, amount),
							transactionId);
					}
					else if (record.DebitCredit == "Af") // Debit - money going out
					{
						yield return PartialActivity.CreateCashWithdrawal(
							currency,
							record.TransactionDate,
							amount,
							new Money(currency, amount),
							transactionId);
					}
					break;

				case "Dividend Uitkering": // Dividend Payment
					if (!string.IsNullOrEmpty(record.FundName))
					{
						var grossAmount = record.GrossAmount ?? Math.Abs(record.NetAmount.GetValueOrDefault());
						var dividendTax = record.DividendTax ?? 0m;

						// Create dividend activity for the net amount
						yield return PartialActivity.CreateDividend(
							currency,
							record.TransactionDate,
							PartialSymbolIdentifier.CreateStockAndETF(stockIdentifier, GetConstructedId(stockIdentifier)),
							Math.Abs(grossAmount),
							new Money(currency, Math.Abs(grossAmount)),
							transactionId);

						// Create tax activity if there's dividend tax
						if (dividendTax > 0)
						{
							yield return PartialActivity.CreateTax(
								currency,
								record.TransactionDate,
								dividendTax,
								new Money(currency, dividendTax),
								transactionId);
						}
					}
					break;

				case "Dividend reservering": // Dividend Reservation (usually 0 amount, just bookkeeping)
											 // This is typically a bookkeeping entry with 0 amount, so we can skip it
					break;

				default:
					throw new NotSupportedException($"Transaction type '{record.TransactionType}' is not supported.");
			}
		}

		private static string? GetConstructedId(string? fundName)
		{
			return fundName?.Trim().ToUpperInvariant().Replace(" ", "");
		}

		protected override Encoding Encoding => Encoding.BigEndianUnicode;

		protected override CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ";"
			};
		}
	}
}