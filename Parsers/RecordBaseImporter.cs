using CsvHelper;
using CsvHelper.Configuration;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers
{
	public abstract class RecordBaseImporter<T> : IFileImporter
	{
		protected RecordBaseImporter()
		{
		}

		public virtual Task<bool> CanParseActivities(string filename)
		{
			try
			{
				CsvConfiguration csvConfig = GetConfig();

				using var streamReader = GetStreamReader(filename);
				using var csvReader = new CsvReader(streamReader, csvConfig);
				csvReader.Read();
				csvReader.ReadHeader();
				csvReader.ValidateHeader<T>();
			}
			catch
			{
				return Task.FromResult(false);
			}

			return Task.FromResult(true);
		}

		public async Task ParseActivities(string filename, HoldingsAndAccountsCollection holdingsAndAccountsCollection, string accountName)
		{
			var csvConfig = GetConfig();
			using var streamReader = GetStreamReader(filename);
			using var csvReader = new CsvReader(streamReader, csvConfig);
			csvReader.Read();
			csvReader.ReadHeader();
			var records = csvReader.GetRecords<T>().ToList();

			var account = holdingsAndAccountsCollection.GetAccount(accountName);
			for (int i = 0; i < records.Count; i++)
			{
				var partialActivity = ParseRow(records[i], i + 1);
				holdingsAndAccountsCollection.AddPartialActivity(partialActivity);
			};
		}

		protected abstract IEnumerable<PartialActivity> ParseRow(T record, int rowNumber);

		protected abstract CsvConfiguration GetConfig();

		protected virtual StreamReader GetStreamReader(string file)
		{
			return File.OpenText(file);
		}
	}
}
