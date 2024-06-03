using CsvHelper;
using CsvHelper.Configuration;
using GhostfolioSidekick.Parsers.Coinbase;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.MacroTrends
{
	public class MacroTrendsParser : IHistoryDataFileImporter
	{
		public Task<bool> CanParse(string filename)
		{
			try
			{
				CsvConfiguration csvConfig = GetConfig();

				using var streamReader = GetStreamReader(filename);
				using var csvReader = new CsvReader(streamReader, csvConfig);
				csvReader.Read();
				csvReader.ReadHeader();

				csvReader.ValidateHeader<MacroTrendsRecord>();
			}
			catch
			{
				return Task.FromResult(false);
			}

			return Task.FromResult(true);
		}

		public Task<IEnumerable<HistoricData>> ParseHistoricData(string filename)
		{
			var csvConfig = GetConfig();
			using var streamReader = GetStreamReader(filename);
			using var csvReader = new CsvReader(streamReader, csvConfig);
			csvReader.Read();
			csvReader.ReadHeader();
			var records = csvReader.GetRecords<MacroTrendsRecord>().ToList();

			var symbol = GetSymbol(filename);

			var lst = new List<HistoricData>();
			for (int i = 0; i < records.Count; i++)
			{
				lst.Add(ParseRow(symbol, records[i]));
			}

			return Task.FromResult<IEnumerable<HistoricData>>(lst);
		}

		private static string GetSymbol(string filename)
		{
			var line = File.ReadAllLines(filename).Skip(1).First();
			return line.Substring(1, line.IndexOf(" ") - 1).Trim();
		}

		private static HistoricData ParseRow(string symbol, MacroTrendsRecord macroTrendsRecord)
		{
			return new HistoricData
			{
				Date = macroTrendsRecord.Date.ToUniversalTime(),
				Open = macroTrendsRecord.Open,
				High = macroTrendsRecord.High,
				Low = macroTrendsRecord.Low,
				Close = macroTrendsRecord.Close,
				Volume = macroTrendsRecord.Volume,
				Symbol = symbol,
			};
		}

		private static StreamReader GetStreamReader(string file)
		{
			return File.OpenText(file);
		}

		private static CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ",",
				ShouldSkipRecord = (r) =>
				{
					return !r.Row[0]!.StartsWith("date") && !r.Row[0]!.StartsWith("19") && !r.Row[0]!.StartsWith("20");
				},
			};
		}
	}
}
