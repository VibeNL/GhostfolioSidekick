using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using GhostfolioSidekick.Ghostfolio.API;

namespace GhostfolioSidekick.FileImporter
{
	public abstract class CSVSingleFileBaseImporter : IFileImporter
	{
		protected readonly IGhostfolioAPI api;

		protected CSVSingleFileBaseImporter(IGhostfolioAPI api)
		{
			this.api = api;
		}

		protected abstract IEnumerable<HeaderMapping> ExpectedHeaders { get; }

		public virtual async Task<bool> CanConvertOrders(IEnumerable<string> filenames)
		{
			foreach (var file in filenames)
			{
				CsvConfiguration csvConfig = GetConfig();

				using var streamReader = File.OpenText(file);
				using var csvReader = new CsvReader(streamReader, csvConfig);

				csvReader.Read();
				csvReader.ReadHeader();

				if (!ExpectedHeaders.All(x => csvReader.HeaderRecord?.Contains(x.SourceName) ?? false))
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
				while (csvReader.Read())
				{
					var asset = await GetAsset(csvReader);

					if (asset == null)
					{
						continue;
					}

					var order = new Order
					{
						AccountId = account.Id,
						Currency = GetValue(csvReader, DestinationHeader.Currency),
						Date = GetDate(csvReader, DestinationHeader.Date),
						Fee = GetFee(csvReader),
						Quantity = GetQuantity(csvReader),
						Asset = asset,
						Type = GetOrderType(csvReader),
						UnitPrice = GetUnitPrice(csvReader),
						Comment = GetComment(csvReader)
					};

					if (order.Type != OrderType.IGNORE)
					{
						list.Add(order);
					}
				}
			}

			return PostProcessList(list);
		}

		protected virtual IEnumerable<Order> PostProcessList(List<Order> list)
		{
			return list;
		}

		protected virtual decimal GetUnitPrice(CsvReader csvReader)
		{
			return GetDecimalValue(csvReader, DestinationHeader.UnitPrice);
		}

		protected virtual decimal GetQuantity(CsvReader csvReader)
		{
			return GetDecimalValue(csvReader, DestinationHeader.Quantity);
		}

		protected abstract string GetComment(CsvReader csvReader);

		protected abstract OrderType GetOrderType(CsvReader csvReader);

		protected abstract decimal GetFee(CsvReader csvReader);

		protected abstract Task<Asset> GetAsset(CsvReader csvReader);

		protected decimal GetDecimalValue(CsvReader csvReader, DestinationHeader header)
		{
			var stringvalue = GetValue(csvReader, header);
			return decimal.Parse(stringvalue, GetCultureForParsingNumbers());
		}

		protected abstract CultureInfo GetCultureForParsingNumbers();

		protected abstract DateTime GetDate(CsvReader csvReader, DestinationHeader header);

		protected string GetValue(CsvReader csvReader, DestinationHeader header)
		{
			return csvReader.GetField(GetSourceFieldName(header));
		}

		protected string GetSourceFieldName(DestinationHeader header)
		{
			return ExpectedHeaders.Single(x => x.DestinationHeader == header).SourceName;
		}

		protected virtual CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ";",
			};
		}
	}
}
