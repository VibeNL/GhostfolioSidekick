using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;

namespace GhostfolioSidekick.Ghostfolio.API.Mapper
{
    internal class SymbolMapper
    {
        string mappingFile = Environment.GetEnvironmentVariable("MAPPINGFILE");

        private Dictionary<Tuple<TypeOfMapping, string>, string> mapping = new Dictionary<Tuple<TypeOfMapping, string>, string>();

        public SymbolMapper()
        {
            if (string.IsNullOrWhiteSpace(mappingFile))
            {
                // No mapping file found
                return;
            }

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
                        mapping.Add(Tuple.Create(Enum.Parse<TypeOfMapping>(csvReader.GetField("TYPE")), csvReader.GetField("SOURCE")), csvReader.GetField("TARGET"));
                    }
                }
            }
        }

        internal string MapCurrency(string sourceCurrency)
        {
            return mapping.GetValueOrDefault(Tuple.Create(TypeOfMapping.CURRENCY, sourceCurrency), sourceCurrency);
        }

        internal string? MapIdentifier(string? identifier)
        {
            return mapping.GetValueOrDefault(Tuple.Create(TypeOfMapping.IDENTIFIER, identifier), identifier);
        }
    }

    internal enum TypeOfMapping
    {
        CURRENCY,

        IDENTIFIER
    }
}
