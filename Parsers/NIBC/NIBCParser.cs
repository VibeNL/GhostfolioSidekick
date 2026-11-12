using CsvHelper;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.NIBC
{
	public class NIBCParser(ICurrencyMapper currencyMapper, IApplicationSettings applicationSettings, ILogger<NIBCParser> logger) : RecordBaseImporter<NIBCRecord>(applicationSettings, logger)
	{
		protected override IEnumerable<PartialActivity> ParseRow(NIBCRecord record, int rowNumber)
		{
			var currency = currencyMapper.Map(record.Currency);
			if (record.Description == "Opening termijndeposito" || record.Description == "Uitkering van termijndeposito")
			{
				// Term deposit rows do not move money in or out of this account.
				return [];
			}

			if (record.Description == "Inkomende overboeking")
			{
				if (record.Amount <= 0)
				{
					throw new NotSupportedException($"Cannot parse {record.Description} with non-positive amount {record.Amount.ToString(CultureInfo.InvariantCulture)} on row {rowNumber}.");
				}

				return [PartialActivity.CreateCashDeposit(
					currency,
					record.Date,
					Math.Abs(record.Amount),
					new Money(currency, Math.Abs(record.Amount)),
					record.TransactionID)];
			}

			if (record.Description == "Uitgaande overboeking")
			{
				return [PartialActivity.CreateCashWithdrawal(
					currency,
					record.Date,
					Math.Abs(record.Amount),
					new Money(currency, Math.Abs(record.Amount)),
					record.TransactionID)];
			}

			if (record.Description == "Renteuitkering" || record.Description == "Bonusrente")
			{
				return [PartialActivity.CreateInterest(
					currency,
					record.Date,
					Math.Abs(record.Amount),
					record.Description,
					new Money(currency, Math.Abs(record.Amount)),
					record.TransactionID)];
			}

			throw new UnknownTransactionTypeException("NIBC", record.Description);
		}

		protected override CsvReaderOptions GetConfig()
		{
			return new CsvReaderOptions
			{
				HasHeader = true,
				PoolStrings = true,
				Separator = ';',
			};
		}

		protected override bool IsHeaderLine(string line) =>
			!line.StartsWith("Nr v/d rekening") &&
			!line.StartsWith("\"Nr v/d rekening");
	}
}
