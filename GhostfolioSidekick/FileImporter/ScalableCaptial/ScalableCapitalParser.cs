using CsvHelper.Configuration;
using CsvHelper;
using GhostfolioSidekick.Ghostfolio.API;
using System.Globalization;
using System.Collections.Generic;

namespace GhostfolioSidekick.FileImporter.ScalableCaptial
{

    public class ScalableCapitalParser : IFileImporter
    {
        private IEnumerable<IFileImporter> fileImporters;

        private IGhostfolioAPI api;

        public ScalableCapitalParser(IGhostfolioAPI api)
        {
            this.api = api;
        }

        public async Task<bool> CanConvertOrders(IEnumerable<string> filenames)
        {
            foreach (var file in filenames)
            {
                CsvConfiguration csvConfig = GetConfig();

                using var streamReader = File.OpenText(file);
                using var csvReader = new CsvReader(streamReader, csvConfig);

                csvReader.Read();
                csvReader.ReadHeader();


                var canParse = IsWUMRecord(csvReader) || IsRKKRecord(csvReader);
                if (!canParse)
                {
                    return false;
                }
            }

            return true;
        }


        public async Task<IEnumerable<Order>> ConvertToOrders(string accountName, IEnumerable<string> filenames)
        {
            var list = new List<Order>();
            var wumRecords = new List<BaaderBankWUMRecord>();
            var rkkRecords = new List<BaaderBankRKKRecord>();

            var account = await api.GetAccountByName(accountName);

            if (account == null)
            {
                throw new NotSupportedException();
            }

            foreach (var filename in filenames)
            {
                CsvConfiguration csvConfig = GetConfig();

                using var streamReader = File.OpenText(filename);
                using var csvReader = new CsvReader(streamReader, csvConfig);

                csvReader.Read();
                csvReader.ReadHeader();

                if (IsWUMRecord(csvReader))
                {
                    wumRecords.AddRange(csvReader.GetRecords<BaaderBankWUMRecord>().ToList());
                }

                if (IsRKKRecord(csvReader))
                {
                    rkkRecords.AddRange(csvReader.GetRecords<BaaderBankRKKRecord>().ToList());
                }
            }

            foreach (var record in wumRecords)
            {
                var order = await ConvertToOrder(account, record, rkkRecords);
                if (order != null)
                {
                    list.Add(order);
                }
            }

            foreach (var record in rkkRecords)
            {
                var order = await ConvertToOrder(account, record);
                if (order != null)
                {
                    list.Add(order);
                }
            }

            return list.DistinctBy(x => new { x.AccountId, x.Asset, x.Currency, x.Date, x.UnitPrice, x.Quantity });
        }

        private async Task<Order> ConvertToOrder(Account account, BaaderBankRKKRecord record)
        {
            var orderType = GetOrderType(record);
            if (orderType == null)
            {
                return null;
            }

            var asset = await api.FindSymbolByISIN(record.Isin.Replace("ISIN ", string.Empty));

            var quantity = decimal.Parse(record.Quantity.Replace("STK ", string.Empty), GetCultureForParsingNumbers());
            var unitPrice = record.UnitPrice.Value / quantity;
            return new Order
            {
                AccountId = account.Id,
                Asset = asset,
                Comment = $"Transaction Reference: [{record.Reference}]",
                Currency = record.Currency,
                Date = record.Date.ToDateTime(TimeOnly.MinValue),
                Fee = 0,
                FeeCurrency = record.Currency,
                Quantity = quantity,
                ReferenceCode = record.Reference,
                Type = orderType.Value,
                UnitPrice = unitPrice
            };
        }

        private async Task<Order> ConvertToOrder(Account account, BaaderBankWUMRecord record, List<BaaderBankRKKRecord> rkkRecords)
        {
            var asset = await api.FindSymbolByISIN(record.Isin);

            var fee = FindFeeRecord(rkkRecords, record.Reference);

            return new Order
            {
                AccountId = account.Id,
                Asset = asset,
                Comment = $"Transaction Reference: [{record.Reference}]",
                Currency = record.Currency,
                Date = record.Date.ToDateTime(TimeOnly.MinValue),
                Fee = Math.Abs(fee?.UnitPrice ?? 0),
                FeeCurrency = fee?.Currency ?? record.Currency,
                Quantity = record.Quantity.Value,
                ReferenceCode = record.Reference,
                Type = GetOrderType(record),
                UnitPrice = record.UnitPrice.Value
            };
        }

        private BaaderBankRKKRecord? FindFeeRecord(List<BaaderBankRKKRecord> rkkRecords, string reference)
        {
            return rkkRecords.FirstOrDefault(x => x.Reference == reference);
        }

        private OrderType GetOrderType(BaaderBankWUMRecord record)
        {
            switch (record.OrderType)
            {
                case "Verkauf":
                    return OrderType.SELL;
                case "Kauf":
                    return OrderType.BUY;
                default:
                    throw new NotSupportedException();
            }
        }

        private OrderType? GetOrderType(BaaderBankRKKRecord record)
        {
            if (record.OrderType == "Coupons/Dividende")
            {
                return OrderType.DIVIDEND;
            }
            
            return null;
        }

        private CsvConfiguration GetConfig()
        {
            return new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                CacheFields = true,
                Delimiter = ";",
            };
        }

        private static bool IsRKKRecord(CsvReader csvReader)
        {
            try
            {
                csvReader.ValidateHeader<BaaderBankRKKRecord>();
                return true;
            }
            catch
            {
            }

            return false;
        }

        private static bool IsWUMRecord(CsvReader csvReader)
        {
            try
            {
                csvReader.ValidateHeader<BaaderBankWUMRecord>();
                return true;
            }
            catch
            {
            }

            return false;
        }

        private CultureInfo GetCultureForParsingNumbers()
        {
            return new CultureInfo("en")
            {
                NumberFormat =
                {
                    NumberDecimalSeparator = ","
                }
            };
        }
    }
}
