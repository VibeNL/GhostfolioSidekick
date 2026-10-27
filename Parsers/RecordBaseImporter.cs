using CsvHelper;
using CsvHelper.Configuration;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model.Activities;
using Microsoft.Extensions.Logging;
using System.Text;

namespace GhostfolioSidekick.Parsers
{
	public abstract class RecordBaseImporter<T> : IActivityFileImporter where T : class
	{
		private readonly Dictionary<string, bool> KnownHeaderCache = [];
		private readonly IApplicationSettings applicationSettings;
		private readonly ILogger logger;

		protected RecordBaseImporter(IApplicationSettings settings, ILogger logger)
		{
			applicationSettings = settings;
			this.logger = logger;
		}

		public virtual Task<bool> CanParse(string filename)
		{
			if (!filename.EndsWith(".csv", StringComparison.InvariantCultureIgnoreCase))
			{
				return Task.FromResult(false);
			}

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

		public Task ParseActivities(string filename, IActivityManager activityManager, string accountName)
		{
			var csvConfig = GetConfig();
			using var streamReader = GetStreamReader(filename);
			using var csvReader = new CsvReader(streamReader, csvConfig);
			csvReader.Read();
			csvReader.ReadHeader();
			var records = csvReader.GetRecords<T>().ToList();

			var warnOnUnsupportedRow = applicationSettings.ConfigurationInstance.Settings.WarnOnUnsupportedRow;

			for (int i = 0; i < records.Count; i++)
			{
				IEnumerable<PartialActivity> partialActivities;
				try
				{
					partialActivities = ParseRow(records[i], i + 1);
				}
				catch (NotSupportedException ex) when (warnOnUnsupportedRow)
				{
					logger.LogWarning("Skipping unsupported row {RowNumber}: {Message}", i + 1, ex.Message);
					continue;
				}

				activityManager.AddPartialActivity(accountName, partialActivities);
			}

			return Task.CompletedTask;
		}

		protected abstract IEnumerable<PartialActivity> ParseRow(T record, int rowNumber);

		protected abstract CsvConfiguration GetConfig();

		protected virtual Encoding Encoding => Encoding.UTF8;

		protected virtual StreamReader GetStreamReader(string file)
		{
			return new StreamReader(file, Encoding, true);
		}
	}
}
