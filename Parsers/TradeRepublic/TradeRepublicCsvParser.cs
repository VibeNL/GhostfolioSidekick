using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class TradeRepublicCsvParser(ICurrencyMapper currencyMapper) : RecordBaseImporter<TradeRepublicCsvRecord>
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
					lst.Add(PartialActivity.CreateCashDeposit(
						currency,
						date,
						Math.Abs(record.Amount.GetValueOrDefault()),
						new Money(currency, Math.Abs(record.Amount.GetValueOrDefault())),
						transactionId));
					break;

				case "CUSTOMER_OUTBOUND_REQUEST":
				case "TRANSFER_INSTANT_OUTBOUND":
				case "PRIVATE_MARKET_BUY":
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
							transactionId + "_TAX"));
					}
					break;

				case "DIVIDEND":
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
							transactionId + "_TAX"));
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
							transactionId + "_FEE"));
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
							transactionId + "_FEE"));
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

				default:
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
