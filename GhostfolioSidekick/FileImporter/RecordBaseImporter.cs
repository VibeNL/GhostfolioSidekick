using CsvHelper;
using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
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

		public virtual async Task<bool> CanParseActivities(IEnumerable<string> filenames)
		{
			foreach (var file in filenames)
			{
				try
				{
					CsvConfiguration csvConfig = GetConfig();

					using var streamReader = GetStreamReader(file);
					using var csvReader = new CsvReader(streamReader, csvConfig);
					csvReader.Read();
					csvReader.ReadHeader();
					csvReader.ValidateHeader<T>();
				}
				catch
				{
					return false;
				}
			}

			return true;
		}

		public async Task<Model.Account> ConvertActivitiesForAccount(string accountName, IEnumerable<string> filenames)
		{
			var account = await api.GetAccountByName(accountName) ?? throw new NotSupportedException($"Account not found {accountName}");
			account.Balance.Empty();

			CsvConfiguration csvConfig = GetConfig();

			var list = new ConcurrentDictionary<string, Model.Activity>();
			foreach (var filename in filenames)
			{
				using var streamReader = GetStreamReader(filename);
				using var csvReader = new CsvReader(streamReader, csvConfig);
				csvReader.Read();
				csvReader.ReadHeader();
				var records = csvReader.GetRecords<T>().ToList();

				foreach (var record in records)
				{
					var orders = await ConvertOrders(record, account, records);

					if (orders != null)
					{
						foreach (var order in orders)
						{
							list.TryAdd(order.ReferenceCode, order);
						}
					}
				};
			};

			SetActivitiesToAccount(account, list.Values);

			return account;
		}

		protected virtual void SetActivitiesToAccount(Model.Account account, ICollection<Model.Activity> values)
		{
			account.ReplaceActivities(values);
		}

		protected abstract Task<IEnumerable<Model.Activity>> ConvertOrders(T record, Model.Account account, IEnumerable<T> allRecords);

		protected abstract CsvConfiguration GetConfig();

		protected virtual StreamReader GetStreamReader(string file)
		{
			return File.OpenText(file);
		}
	}
}
