using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Generic
{
	public class GenericParser : RecordBaseImporter<GenericRecord>
	{
		public GenericParser()
		{
		}

		protected override IEnumerable<PartialActivity> ParseRow(GenericRecord record, int rowNumber)
		{
			if (string.IsNullOrWhiteSpace(record.Id))
			{
				record.Id = $"{record.ActivityType}_{record.Symbol}_{record.Date.ToInvariantDateOnlyString()}_{record.Quantity.ToString(CultureInfo.InvariantCulture)}_{record.Currency}_{record.Fee?.ToString(CultureInfo.InvariantCulture)}";
			}

			var lst = new List<PartialActivity>();
			var currency = new Currency(record.Currency);
			var unitPrice = record.UnitPrice;

			if (record.Tax != null && record.Tax != 0)
			{
				lst.Add(PartialActivity.CreateTax(currency, record.Date, record.Tax.Value, record.Id));
			}
			if (record.Fee != null && record.Fee != 0)
			{
				lst.Add(PartialActivity.CreateFee(currency, record.Date, record.Fee.Value, record.Id));
			}

			switch (record.ActivityType)
			{
				case ActivityType.Receive:
				case ActivityType.Buy:
					lst.Add(PartialActivity.CreateBuy(currency, record.Date,
						[PartialSymbolIdentifier.CreateGeneric(record.Symbol!)], record.Quantity, unitPrice, record.Id));
					break;
				case ActivityType.Send:
				case ActivityType.Sell:
					lst.Add(PartialActivity.CreateSell(currency, record.Date, [PartialSymbolIdentifier.CreateGeneric(record.Symbol!)], record.Quantity, unitPrice, record.Id));
					break;
				case ActivityType.Dividend:
					lst.Add(PartialActivity.CreateDividend(currency, record.Date, [PartialSymbolIdentifier.CreateGeneric(record.Symbol!)], record.Quantity * record.UnitPrice, record.Id));
					break;
				case ActivityType.Interest:
					lst.Add(PartialActivity.CreateInterest(currency, record.Date, record.UnitPrice, record.Id));
					break;
				case ActivityType.Fee:
					if (record.UnitPrice != 0)
					{
						lst.Add(PartialActivity.CreateFee(currency, record.Date, record.UnitPrice, record.Id));
					}
					break;
				case ActivityType.Valuable:
					lst.Add(PartialActivity.CreateValuable(currency, record.Date, record.Symbol!, record.Quantity * record.UnitPrice, record.Id));
					break;
				case ActivityType.Liability:
					lst.Add(PartialActivity.CreateLiability(currency, record.Date, record.Symbol!, record.Quantity * record.UnitPrice, record.Id));
					break;
				case ActivityType.Gift:
					lst.Add(PartialActivity.CreateGift(currency, record.Date, record.UnitPrice, record.Id));
					break;
				case ActivityType.CashDeposit:
					lst.Add(PartialActivity.CreateCashDeposit(currency, record.Date, record.UnitPrice, record.Id));
					break;
				case ActivityType.CashWithdrawal:
					lst.Add(PartialActivity.CreateCashWithdrawal(currency, record.Date, record.UnitPrice, record.Id));
					break;
				case ActivityType.Tax:
					if (record.UnitPrice != 0)
					{
						lst.Add(PartialActivity.CreateTax(currency, record.Date, record.UnitPrice, record.Id));
					}
					break;
				case ActivityType.LearningReward:
				case ActivityType.StakingReward:
				case ActivityType.Convert:
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
