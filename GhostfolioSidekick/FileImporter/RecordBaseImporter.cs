using CsvHelper.Configuration;
using CsvHelper;
using GhostfolioSidekick.Ghostfolio.API;

namespace GhostfolioSidekick.FileImporter
{
    public abstract class RecordBaseImporter<T> : IFileImporter
	{
		protected readonly IGhostfolioAPI api;

		protected RecordBaseImporter(IGhostfolioAPI api)
		{
			this.api = api;
		}

		public virtual async Task<bool> CanConvertOrders(IEnumerable<string> filenames)
		{
			foreach (var file in filenames)
			{
				CsvConfiguration csvConfig = GetConfig();

				using var streamReader = File.OpenText(file);
				using var csvReader = new CsvReader(streamReader, csvConfig);

				csvReader.Read();
				csvReader.ReadHeader();

				try
				{
					csvReader.ValidateHeader<T>();
				} catch
				{
					return false;
				}
			}

			return true;
		}

		public async Task<IEnumerable<Order>> ConvertToOrders(string accountName, IEnumerable<string> filenames)
		{
			var list = new List<Order>();
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
				var records = csvReader.GetRecords<T>().ToList();

				foreach (var record in records)
				{
					var order = await ConvertOrder(record, account, records);

					if (order!= null)
					{
						list.Add(order);
					}
				}
			}

			return list;
		}

        protected abstract Task<Order?> ConvertOrder(T record, Account account, IEnumerable<T> allRecords);

        protected abstract CsvConfiguration GetConfig();
	}
}
