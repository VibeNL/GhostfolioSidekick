using CsvHelper;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model.Activities;
using Microsoft.Extensions.Logging;
using System.Text;

namespace GhostfolioSidekick.Parsers
{
	public abstract class RecordBaseImporter<T> : IActivityFileImporter where T : class
	{
		private readonly Dictionary<string, bool> KnownHeaderCache = [];

		private readonly IApplicationSettings? applicationSettings;
		private readonly ILogger? logger;

		protected RecordBaseImporter(IApplicationSettings? applicationSettings = null, ILogger? logger = null)
		{
			this.applicationSettings = applicationSettings;
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
				using var csvReader = CreateReader(filename, headerOnly: true);
				var isValid = csvReader.TryValidateHeader<T>(out _);

				record = string.Join("|", csvReader.Headers);

				if (KnownHeaderCache.TryGetValue(record, out var canParse))
				{
					return Task.FromResult(canParse);
				}

				if (!isValid)
				{
					KnownHeaderCache.Add(record, false);
					return Task.FromResult(false);
				}
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
			using var csvReader = CreateReader(filename);
			var records = csvReader.GetRecords<T>().ToList();

			for (int i = 0; i < records.Count; i++)
			{
				var rowNumber = i + 1;
				List<PartialActivity> partialActivity;
				try
				{
					partialActivity = [.. ParseRow(records[i], rowNumber)];
				}
				catch (UnknownTransactionTypeException ex) when (applicationSettings?.ConfigurationInstance.Settings.SkipUnknownTransactionTypes == true)
				{
					logger?.LogWarning(ex, "Skipping unsupported {Parser} transaction type '{Type}' on row {RowNumber} in {File}.", ex.ParserName, ex.TransactionType, rowNumber, filename);
					continue;
				}

				activityManager.AddPartialActivity(accountName, partialActivity);
			}

			return Task.CompletedTask;
		}

		protected abstract IEnumerable<PartialActivity> ParseRow(T record, int rowNumber);

		protected abstract CsvReaderOptions GetConfig();

		protected virtual bool IsHeaderLine(string line) => true;

		private CsvReader CreateReader(string filename, bool headerOnly = false)
		{
			using var streamReader = GetStreamReader(filename);
			var builder = new StringBuilder();
			var headerFound = false;
			string? line;
			while ((line = streamReader.ReadLine()) != null)
			{
				headerFound = headerFound || IsHeaderLine(line);
				if (headerFound)
				{
					builder.Append(line).Append('\n');
					if (headerOnly)
					{
						break;
					}
				}
			}

			return CsvReader.FromText(builder.ToString(), GetConfig());
		}

		protected virtual Encoding Encoding => Encoding.UTF8;

		protected virtual StreamReader GetStreamReader(string file)
		{
			return new StreamReader(file, Encoding, true);
		}
	}
}
