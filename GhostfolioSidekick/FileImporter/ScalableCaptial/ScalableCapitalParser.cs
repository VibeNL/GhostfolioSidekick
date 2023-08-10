using CsvHelper.Configuration;
using CsvHelper;
using GhostfolioSidekick.Ghostfolio.API;
using System.Globalization;

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

            foreach (var filename in filenames)
            {
                var account = await api.GetAccountByName(accountName);

                if (account == null)
                {
                    throw new NotSupportedException();
                }

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
                list.AddRange(ConvertToOrder(record, rkkRecords));
            }

            foreach (var record in rkkRecords)
            {
                list.AddRange(ConvertToOrder(record));
            }

            return list;
        }

        private IEnumerable<Order> ConvertToOrder(BaaderBankRKKRecord record)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Order> ConvertToOrder(BaaderBankWUMRecord record, List<BaaderBankRKKRecord> rkkRecords)
        {
            throw new NotImplementedException();
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
    }
}
