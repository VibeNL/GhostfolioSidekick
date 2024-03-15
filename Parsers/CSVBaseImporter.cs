using CsvHelper;
using CsvHelper.Configuration;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers
{
	public abstract class CSVBaseImporter<T> : IFileImporter
	{
		private readonly Dictionary<string, bool> KnownHeaderCache = new Dictionary<string, bool>();

		protected CSVBaseImporter()
		{
		}

		public virtual Task<bool> CanParseActivities(string filename)
		{
			string? record = null;

			try
			{
				CsvConfiguration csvConfig = GetConfig();

				using var streamReader = GetStreamReader(filename);
				using var csvReader = new CsvReader(streamReader, csvConfig);
				csvReader.Read();
				csvReader.ReadHeader();

				record = string.Join("|", csvReader.HeaderRecord!);

				if (KnownHeaderCache.TryGetValue(record, out var canParse))
				{
					return Task.FromResult(canParse);
				}

				csvReader.ValidateHeader<T>();
			}
			catch
			{
				if (record != null)
				{
					KnownHeaderCache.Add(record, false);
				}

				return Task.FromResult(false);
			}

			KnownHeaderCache.Add(record, true);
			return Task.FromResult(true);
		}

		public Task ParseActivities(string filename, IHoldingsCollection holdingsAndAccountsCollection, string accountName)
		{
			var csvConfig = GetConfig();
			using var streamReader = GetStreamReader(filename);
			using var csvReader = new CsvReader(streamReader, csvConfig);
			csvReader.Read();
			csvReader.ReadHeader();
			var records = csvReader.GetRecords<T>().ToList();

			for (int i = 0; i < records.Count; i++)
			{
				var partialActivity = ParseRow(records[i], i + 1);
				holdingsAndAccountsCollection.AddPartialActivity(accountName, partialActivity);
			}

			return Task.CompletedTask;
		}

		protected abstract IEnumerable<PartialActivity> ParseRow(T record, int rowNumber);

		protected abstract CsvConfiguration GetConfig();

		protected virtual StreamReader GetStreamReader(string file)
		{
			return File.OpenText(file);
		}
	}
}
