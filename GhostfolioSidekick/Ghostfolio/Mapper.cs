using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;

namespace GhostfolioSidekick.Ghostfolio
{
	internal class Mapper
    {
        string mappingFile = Environment.GetEnvironmentVariable("MAPPINGFILE");

        private Dictionary<string, string> mapping = new Dictionary<string, string>();

        public Mapper()
        {
            using (var streamReader = File.OpenText(mappingFile))
            {
                using (var csvReader = new CsvReader(streamReader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    CacheFields = true,
                    Delimiter = ",",
                }))
                {
                    csvReader.Read();
                    csvReader.ReadHeader();

                    while (csvReader.Read())
                    {
                        mapping.Add(csvReader.GetField(0), csvReader.GetField(1));
                    }
                }
            }
        }

        internal string? MapIdentifier(string? identifier)
        {
            return mapping.GetValueOrDefault(identifier, identifier);
        }
    }
}
