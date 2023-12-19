using CsvHelper;
using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using System.Collections.Concurrent;

namespace GhostfolioSidekick.FileImporter
{
	public abstract class RecordBaseImporter<T> : IFileImporter
	{
		protected readonly IGhostfolioAPI api;

		protected RecordBaseImporter(IGhostfolioAPI api)
		{
			this.api = api;
		}

		public virtual async Task<bool> CanParseActivities(string fileName)
		{
			try
			{
				CsvConfiguration csvConfig = GetConfig();

				using var streamReader = GetStreamReader(fileName);
				using var csvReader = new CsvReader(streamReader, csvConfig);
				csvReader.Read();
				csvReader.ReadHeader();
				csvReader.ValidateHeader<T>();
			}
			catch
			{
				return false;
			}

			return true;
		}

		public async Task<IEnumerable<Activity>> ConvertToActivities(string fileName, Currency defaultCurrency)
		{
			CsvConfiguration csvConfig = GetConfig();

			var list = new ConcurrentDictionary<string, Activity>();
			using var streamReader = GetStreamReader(fileName);
			using var csvReader = new CsvReader(streamReader, csvConfig);
			csvReader.Read();
			csvReader.ReadHeader();
			var records = csvReader.GetRecords<T>().ToList();

			foreach (var record in records)
			{
				var orders = await ConvertOrders(record, records, defaultCurrency);

				if (orders != null)
				{
					foreach (var order in orders)
					{
						list.TryAdd(order.ReferenceCode, order);
					}
				}
			};

			return list.Values.ToList();
		}

		protected abstract Task<IEnumerable<Activity>> ConvertOrders(T record, IEnumerable<T> allRecords, Currency defaultCurrency);

		protected abstract CsvConfiguration GetConfig();

		protected virtual StreamReader GetStreamReader(string file)
		{
			return File.OpenText(file);
		}
	}
}
