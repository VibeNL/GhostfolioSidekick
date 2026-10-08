using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Trine
{
	public class TrineParser() : RecordBaseImporter<TrineRecord>
	{
		protected override IEnumerable<PartialActivity> ParseRow(TrineRecord record, int rowNumber)
		{
			if (record.Type == "Committed investment")
			{
				yield break;
			}

			var date = record.Date ?? throw new NotSupportedException($"Cannot parse {record.Type} without a date on row {rowNumber}.");

			var transactionsId = $"{record.Type}_{record.Loan}_{date.ToInvariantDateOnlyString()}_{record.Total.ToString(CultureInfo.InvariantCulture)}";

			yield return PartialActivity.CreateKnownBalance(
				Currency.EUR,
				date,
				record.Total
			);

			switch (record.Type)
			{
				case "Investment":
					yield return PartialActivity.CreateBuy(
						Currency.EUR,
						date,
						[PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, record.Loan!, Currency.EUR)],
						1,
						new Money(Currency.EUR, record.OutstandingPortfolioChange),
						new Money(Currency.EUR, record.OutstandingPortfolioChange),
						transactionsId
					);
					break;
				case "Repayment":
					yield return PartialActivity.CreateDividend(
						Currency.EUR,
						date,
						[PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, record.Loan!, Currency.EUR)],
						record.RepaidInterest.GetValueOrDefault(),
						new Money(Currency.EUR, record.RepaidInterest.GetValueOrDefault()),
						transactionsId
					);
					yield return PartialActivity.CreateSell(
						Currency.EUR,
						date,
						[PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, record.Loan!, Currency.EUR)],
						1,
						new Money(Currency.EUR, record.RepaidCapital.GetValueOrDefault()),
						new Money(Currency.EUR, record.RepaidCapital.GetValueOrDefault()),
						transactionsId
					);
					break;
				case "Withdrawal":
					yield return PartialActivity.CreateCashWithdrawal(
						Currency.EUR,
						date,
						record.AvailableBalanceChange * -1,
						new Money(Currency.EUR, record.AvailableBalanceChange * -1),
						transactionsId
					);
					break;
				case string when record.Type.Contains("Activated friend voucher from"):
					yield return PartialActivity.CreateGift(
						Currency.EUR,
						date,
						record.AvailableBalanceChange,
						new Money(Currency.EUR, record.AvailableBalanceChange),
						transactionsId);
					break;

				default:
					throw new NotSupportedException();
			}
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
