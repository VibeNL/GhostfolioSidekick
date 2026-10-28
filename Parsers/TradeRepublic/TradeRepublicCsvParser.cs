using CsvHelper.Configuration;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class TradeRepublicCsvParser(ICurrencyMapper currencyMapper, IApplicationSettings applicationSettings, ILogger<TradeRepublicCsvParser> logger) : RecordBaseImporter<TradeRepublicCsvRecord>
	{
		protected override IEnumerable<PartialActivity> ParseRow(TradeRepublicCsvRecord record, int rowNumber)
		{
			var currency = currencyMapper.Map(record.Currency);
			var date = record.DateTime;
			var transactionId = record.TransactionId;
			var lst = new List<PartialActivity>();

			switch (record.Type)
			{
				case "CUSTOMER_INPAYMENT":
				case "CUSTOMER_INBOUND":
				case "BENEFITS_SAVEBACK":
				case "BONUS":
				case "STOCKPERK":
				case "TRANSFER_INSTANT_INBOUND":
				case "TRANSFER_INBOUND": // Regular (non-instant) incoming transfer, same as the instant variant
					lst.Add(PartialActivity.CreateCashDeposit(
						currency,
						date,
						Math.Abs(record.Amount.GetValueOrDefault()),
						new Money(currency, Math.Abs(record.Amount.GetValueOrDefault())),
						transactionId));
					break;

				case "CUSTOMER_OUTBOUND_REQUEST":
				case "TRANSFER_INSTANT_OUTBOUND":
				case "TRANSFER_OUTBOUND": // Regular (non-instant) outgoing transfer, same as the instant variant
					lst.Add(PartialActivity.CreateCashWithdrawal(
						currency,
						date,
						Math.Abs(record.Amount.GetValueOrDefault()),
						new Money(currency, Math.Abs(record.Amount.GetValueOrDefault())),
						transactionId));
					break;

				case "INTEREST_PAYMENT":
						lst.Add(PartialActivity.CreateInterest(
							currency,
							date,
							record.Amount.GetValueOrDefault(),
							record.Description ?? "Interest payment",
							new Money(currency, record.Amount.GetValueOrDefault()),
							transactionId));
						if (record.Tax.HasValue && record.Tax.Value != 0)
						{
							lst.Add(PartialActivity.CreateTax(
								currency,
								date,
								Math.Abs(record.Tax.Value),
								new Money(currency, Math.Abs(record.Tax.Value)),
								transactionId));
						}
						break;

					case "DIVIDEND":
					case "DISTRIBUTION": // Payout of a distributing item (e.g. ETF), semantically the same as DIVIDEND including the tax column
						var dividendSymbolIds = CreateSymbolIdentifiers(record, currency);
						lst.Add(PartialActivity.CreateDividend(
							currency,
							date,
							dividendSymbolIds,
							record.Amount.GetValueOrDefault(),
							new Money(currency, record.Amount.GetValueOrDefault()),
							transactionId));
						if (record.Tax.HasValue && record.Tax.Value != 0)
						{
							lst.Add(PartialActivity.CreateTax(
								currency,
								date,
								Math.Abs(record.Tax.Value),
								new Money(currency, Math.Abs(record.Tax.Value)),
								transactionId));
						}
						break;

				case "BUY":
					var buySymbolIds = CreateSymbolIdentifiers(record, currency);
					lst.Add(PartialActivity.CreateBuy(
						currency,
						date,
						buySymbolIds,
						record.Shares.GetValueOrDefault(),
						new Money(currency, record.Price.GetValueOrDefault()),
						new Money(currency, Math.Abs(record.Amount.GetValueOrDefault())),
						transactionId));
					if (record.Fee.HasValue && record.Fee.Value != 0)
						{
							lst.Add(PartialActivity.CreateFee(
								currency,
								date,
								Math.Abs(record.Fee.Value),
								new Money(currency, Math.Abs(record.Fee.Value)),
								transactionId));
						}
						break;

					case "SELL":
						var sellSymbolIds = CreateSymbolIdentifiers(record, currency);
						lst.Add(PartialActivity.CreateSell(
							currency,
							date,
							sellSymbolIds,
							Math.Abs(record.Shares.GetValueOrDefault()),
							new Money(currency, record.Price.GetValueOrDefault()),
							new Money(currency, Math.Abs(record.Amount.GetValueOrDefault())),
							transactionId));
						if (record.Fee.HasValue && record.Fee.Value != 0)
						{
							lst.Add(PartialActivity.CreateFee(
								currency,
								date,
								Math.Abs(record.Fee.Value),
								new Money(currency, Math.Abs(record.Fee.Value)),
								transactionId));
						}
						break;

				case "CARD_TRANSACTION":
				case "CARD_TRANSACTION_INTERNATIONAL":
					lst.Add(PartialActivity.CreateCashWithdrawal(
						currency,
						date,
						Math.Abs(record.Amount.GetValueOrDefault()),
						new Money(currency, Math.Abs(record.Amount.GetValueOrDefault())),
						transactionId));
					break;

				case "REDEMPTION":
					// Bond redemption: shares are negative, price is par value
					var redemptionSymbolIds = CreateSymbolIdentifiers(record, currency);
					var redemptionShares = Math.Abs(record.Shares.GetValueOrDefault());
					var redemptionPrice = record.Price.GetValueOrDefault();
					var redemptionTotal = redemptionShares * redemptionPrice;
					lst.Add(PartialActivity.CreateSell(
						currency,
						date,
						redemptionSymbolIds,
						redemptionShares,
						new Money(currency, redemptionPrice),
						new Money(currency, redemptionTotal),
						transactionId));
					break;

				case "FINAL_MATURITY":
					// Ignored: cash is accounted for by the REDEMPTION sell.
					break;
				case "MIGRATION":
					// Ignored: this is just a transfer of existing holdings to the new Trade Republic system, no cash or shares are changing hands.
					break;
				case "PRIVATE_MARKET_BUY":
					// Ignored: this is a cash pre-payment reservation that gets settled by the actual TRADING/BUY entry, which handles the cash reduction.
					break;

				case "PRE_DETERMINED_TAX_BASE":
				case "EARNINGS": // Vorabpauschale (advance tax on accumulating funds); booked as EARNINGS from 2025 onwards
					if (record.Tax.HasValue && record.Tax.Value != 0)
					{
						lst.Add(PartialActivity.CreateTax(
							currency,
							date,
							CreateSymbolIdentifiers(record, currency),
							Math.Abs(record.Tax.Value),
							new Money(currency, Math.Abs(record.Tax.Value)),
							transactionId));
					}
					break;

				case "TAX_OPTIMIZATION": // Tax refund/correction; sign of amount determines direction
				case "INTEREST_PAYMENT_CANCELLED": // Reversal of an interest payment (correction from Trade Republic)
				case "MANUAL_CASH_TRANSFER":
				case "MANUAL_CASH_TRANSFER_CANCELLED":
					if (record.Amount.HasValue && record.Amount.Value != 0)
					{
						lst.Add(CreateCashMovementBySign(currency, date, record.Amount.Value, transactionId));
					}
					break;

				default:
					if (applicationSettings.ConfigurationInstance.Settings.SkipUnknownTransactionTypes)
					{
						logger.LogWarning("Skipping unsupported Trade Republic CSV transaction type '{Type}' on row {RowNumber}.", record.Type, rowNumber);
						return [];
					}

					throw new NotSupportedException($"Trade Republic CSV transaction type '{record.Type}' is not supported.");
			}

			return lst;
		}

		private static ICollection<PartialSymbolIdentifier?> CreateSymbolIdentifiers(TradeRepublicCsvRecord record, Currency currency)
		{
			return
			[
				PartialSymbolIdentifier.CreateStockBondAndETF(IdentifierType.ISIN, record.Symbol!, currency),
				PartialSymbolIdentifier.CreateStockBondAndETF(IdentifierType.Name, record.Name!, currency),
			];
		}

		private static PartialActivity CreateCashMovementBySign(Currency currency, DateTime date, decimal amount, string transactionId)
		{
			var absoluteAmount = Math.Abs(amount);
			return amount >= 0
				? PartialActivity.CreateCashDeposit(currency, date, absoluteAmount, new Money(currency, absoluteAmount), transactionId)
				: PartialActivity.CreateCashWithdrawal(currency, date, absoluteAmount, new Money(currency, absoluteAmount), transactionId);
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
	}
}
