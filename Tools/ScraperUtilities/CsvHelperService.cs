using CsvHelper;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Parsers.Generic;
using System.Globalization;

namespace GhostfolioSidekick.Tools.ScraperUtilities
{
    public class CsvHelperService
    {
        public void SaveToCSV(string outputFile, IEnumerable<ActivityWithSymbol> transactions)
        {
            using var writer = new StreamWriter(outputFile);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(transactions.Select(Transform).OrderBy(x => x.Date));
        }

        private static GenericRecord Transform(ActivityWithSymbol activity)
        {
            if (activity.Activity is BuySellActivity buyActivity && buyActivity.Quantity > 0)
            {
                return new GenericRecord
                {
                    ActivityType = PartialActivityType.Buy,
                    Symbol = activity.Symbol,
                    Date = buyActivity.Date,
                    Currency = buyActivity.UnitPrice.Currency.Symbol,
                    Quantity = buyActivity.Quantity,
                    UnitPrice = buyActivity.UnitPrice.Amount,
                    Fee = Sum(buyActivity.Fees.Select(x => x.Money)),
                    Tax = Sum(buyActivity.Taxes.Select(x => x.Money)),
                };
            }

            if (activity.Activity is BuySellActivity sellActivity && sellActivity.Quantity < 0)
            {
                return new GenericRecord
                {
                    ActivityType = PartialActivityType.Sell,
                    Symbol = activity.Symbol,
                    Date = sellActivity.Date,
                    Currency = sellActivity.UnitPrice.Currency.Symbol,
                    Quantity = sellActivity.Quantity * -1,
                    UnitPrice = sellActivity.UnitPrice.Amount,
                    Fee = Sum(sellActivity.Fees.Select(x => x.Money)),
                    Tax = Sum(sellActivity.Taxes.Select(x => x.Money)),
                };
            }

            if (activity.Activity is DividendActivity dividendActivity)
            {
                return new GenericRecord
                {
                    ActivityType = PartialActivityType.Dividend,
                    Symbol = activity.Symbol,
                    Date = dividendActivity.Date,
                    Currency = dividendActivity.Amount.Currency.Symbol,
                    Quantity = 1,
                    UnitPrice = dividendActivity.Amount.Amount,
                    Fee = Sum(dividendActivity.Fees.Select(x => x.Money)),
                    Tax = Sum(dividendActivity.Taxes.Select(x => x.Money)),
                };
            }

            if (activity.Activity is CashDepositWithdrawalActivity deposit && deposit.Amount.Amount > 0)
            {
                return new GenericRecord
                {
                    ActivityType = PartialActivityType.CashDeposit,
                    Symbol = activity.Symbol,
                    Date = deposit.Date,
                    Currency = deposit.Amount.Currency.Symbol,
                    Quantity = 0,
                    UnitPrice = deposit.Amount.Amount,
                    Fee = 0,
                    Tax = 0,
                };
            }

            if (activity.Activity is CashDepositWithdrawalActivity withdrawal && withdrawal.Amount.Amount < 0)
            {
                return new GenericRecord
                {
                    ActivityType = PartialActivityType.CashWithdrawal,
                    Symbol = activity.Symbol,
                    Date = withdrawal.Date,
                    Currency = withdrawal.Amount.Currency.Symbol,
                    Quantity = 0,
                    UnitPrice = withdrawal.Amount.Amount,
                    Fee = 0,
                    Tax = 0,
                };
            }

            if (activity.Activity is GiftAssetActivity giftAsset)
            {
                return new GenericRecord
                {
                    ActivityType = PartialActivityType.GiftAsset,
                    Symbol = activity.Symbol,
                    Date = giftAsset.Date,
                    Currency = giftAsset.UnitPrice.Currency.Symbol,
                    Quantity = giftAsset.Quantity,
                    UnitPrice = giftAsset.UnitPrice.Amount,
                    Fee = 0,
                    Tax = 0,
                };
            }

            if (activity.Activity is GiftFiatActivity giftFiat)
            {
                return new GenericRecord
                {
                    ActivityType = PartialActivityType.GiftAsset,
                    Symbol = activity.Symbol,
                    Date = giftFiat.Date,
                    Currency = giftFiat.Amount.Currency.Symbol,
                    Quantity = 1,
                    UnitPrice = giftFiat.Amount.Amount,
                    Fee = 0,
                    Tax = 0,
                };
            }

            if (activity.Activity is InterestActivity interestActivity)
            {
                return new GenericRecord
                {
                    ActivityType = PartialActivityType.Interest,
                    Symbol = activity.Symbol,
                    Date = interestActivity.Date,
                    Currency = interestActivity.Amount.Currency.Symbol,
                    Quantity = 1,
                    UnitPrice = interestActivity.Amount.Amount,
                    Fee = 0,
                    Tax = 0,
                };
            }

            throw new ArgumentException("Invalid activity type.");
        }

        private static decimal? Sum(IEnumerable<Money> moneys)
        {
            var currency = moneys.Select(x => x.Currency.Symbol).Distinct().SingleOrDefault();
            if (currency == null)
            {
                return null;
            }

            return moneys.Sum(x => x.Amount);
        }
    }
}
