using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Generic
{
	public class GenericParser : RecordBaseImporter<GenericRecord>
	{
		private readonly ICurrencyMapper currencyMapper;

		public GenericParser(ICurrencyMapper currencyMapper)
		{
			this.currencyMapper = currencyMapper;
		}

		protected override IEnumerable<PartialActivity> ParseRow(GenericRecord record, int rowNumber)
		{
			if (string.IsNullOrWhiteSpace(record.Id))
			{
				record.Id = $"{record.ActivityType}_{record.Symbol}_{record.Date.ToInvariantDateOnlyString()}_{record.Quantity.ToString(CultureInfo.InvariantCulture)}_{record.Currency}_{record.Fee?.ToString(CultureInfo.InvariantCulture)}";
			}

			var lst = new List<PartialActivity>();
			var currency = currencyMapper.Map(record.Currency);
			var unitPrice = record.UnitPrice;

			if (record.Tax != null && record.Tax != 0)
			{
				lst.Add(PartialActivity.CreateTax(currency, record.Date, record.Tax.Value, new Money(currency, record.Tax.Value), record.Id));
			}
			if (record.Fee != null && record.Fee != 0)
			{
				lst.Add(PartialActivity.CreateFee(currency, record.Date, record.Fee.Value, new Money(currency, record.Fee.Value), record.Id));
			}

			switch (record.ActivityType)
			{
				case PartialActivityType.Receive:
					lst.Add(PartialActivity.CreateReceive(
						record.Date,
						[PartialSymbolIdentifier.CreateGeneric(record.Symbol!)],
						record.Quantity,
						record.Id));
					break;
				case PartialActivityType.Buy:
					lst.Add(PartialActivity.CreateBuy(
						currency,
						record.Date,
						[PartialSymbolIdentifier.CreateGeneric(record.Symbol!)],
						record.Quantity,
						unitPrice,
						new Money(currency, Math.Abs(record.TotalTransactionAmount ?? record.Quantity * record.UnitPrice)),
						record.Id));
					break;
				case PartialActivityType.Send:
					lst.Add(PartialActivity.CreateSend(
						record.Date,
						[PartialSymbolIdentifier.CreateGeneric(record.Symbol!)],
						record.Quantity,
						record.Id));
					break;
				case PartialActivityType.Sell:
					lst.Add(PartialActivity.CreateSell(
						currency,
						record.Date,
						[PartialSymbolIdentifier.CreateGeneric(record.Symbol!)],
						record.Quantity,
						unitPrice,
						new Money(currency, Math.Abs(record.TotalTransactionAmount ?? record.Quantity * record.UnitPrice)),
						record.Id));
					break;
				case PartialActivityType.Dividend:
					lst.Add(PartialActivity.CreateDividend(
						currency,
						record.Date,
						[PartialSymbolIdentifier.CreateGeneric(record.Symbol!)],
						record.Quantity * record.UnitPrice,
						new Money(currency, record.TotalTransactionAmount ?? record.Quantity * record.UnitPrice),
						record.Id));
					break;
				case PartialActivityType.Interest:
					lst.Add(PartialActivity.CreateInterest(
						currency,
						record.Date,
						record.UnitPrice,
						"Interest",
						new Money(currency, record.TotalTransactionAmount ?? record.UnitPrice),
						record.Id));
					break;
				case PartialActivityType.Fee:
					if (record.UnitPrice != 0)
					{
						lst.Add(PartialActivity.CreateFee(
							currency,
							record.Date,
							record.UnitPrice,
							new Money(currency, record.TotalTransactionAmount ?? record.UnitPrice),
							record.Id));
					}
					break;
				case PartialActivityType.Valuable:
					lst.Add(PartialActivity.CreateValuable(
						currency,
						record.Date,
						record.Symbol!,
						record.Quantity * record.UnitPrice,
						new Money(currency, record.TotalTransactionAmount ?? record.Quantity * record.UnitPrice),
						record.Id));
					break;
				case PartialActivityType.Liability:
					lst.Add(PartialActivity.CreateLiability(
						currency,
						record.Date,
						record.Symbol!,
						record.Quantity * record.UnitPrice,
						new Money(currency, record.TotalTransactionAmount ?? record.Quantity * record.UnitPrice),
						record.Id));
					break;
				case PartialActivityType.Gift:
					lst.Add(PartialActivity.CreateGift(
						currency,
						record.Date,
						record.UnitPrice,
						new Money(currency, record.TotalTransactionAmount ?? record.Quantity * record.UnitPrice),
						record.Id));
					break;
				case PartialActivityType.CashDeposit:
					lst.Add(PartialActivity.CreateCashDeposit(
						currency,
						record.Date,
						record.UnitPrice,
						new Money(currency, record.TotalTransactionAmount ?? record.Quantity * record.UnitPrice),
						record.Id));
					break;
				case PartialActivityType.CashWithdrawal:
					lst.Add(PartialActivity.CreateCashWithdrawal(
						currency,
						record.Date,
						record.UnitPrice,
						new Money(currency, record.TotalTransactionAmount ?? record.Quantity * record.UnitPrice),
						record.Id));
					break;
				case PartialActivityType.Tax:
					if (record.UnitPrice != 0)
					{
						lst.Add(PartialActivity.CreateTax(
							currency,
							record.Date,
							record.UnitPrice,
							new Money(currency, record.TotalTransactionAmount ?? record.Quantity * record.UnitPrice),
							record.Id));
					}
					break;
				default:
					throw new NotSupportedException();
			}

			return lst.ToList();
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
