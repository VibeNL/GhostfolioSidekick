using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Services
{
	public class ServerCurrencyConversion : IServerCurrencyConversion
	{
		private readonly ICurrencyExchange _currencyExchange;
		private readonly ILogger<ServerCurrencyConversion> _logger;

		private readonly Dictionary<string, ConvertionInfo> Convertions = new()
		{
			{ "CalculatedSnapshotPrimaryCurrency", new ConvertionInfo("CalculatedSnapshot", ConvertCalculatedSnapshot) }
		};

		private static async Task<Dictionary<string, object>> ConvertCalculatedSnapshot(Dictionary<string, object> dictionary, ICurrencyExchange currencyExchange, Currency currency)
		{
			var dict = new Dictionary<string, object>();

			foreach (var kvp in dictionary)
			{
				if (kvp.Value is null)
				{
					dict[kvp.Key] = kvp.Value!;
					continue;
				}

				if (kvp.Key is "AverageCostPrice" or "CurrentUnitPrice" or "TotalInvested" or "TotalValue")
				{
					if (kvp.Value is not string strValue)
					{
						throw new InvalidOperationException($"Expected Money value in string format for key '{kvp.Key}', but got: {kvp.Value?.GetType().Name ?? "null"}");
					}

					var converted = await currencyExchange.ConvertMoney((Money)kvp.Value, currency, DateOnly.FromDateTime(DateTime.UtcNow));
					dict[kvp.Key] = converted.Amount;
				}
				else
				{
					dict[kvp.Key] = kvp.Value;
				}
			}

			return dict;
		}

		public ServerCurrencyConversion(ICurrencyExchange currencyExchange, ILogger<ServerCurrencyConversion> logger)
		{
			_currencyExchange = currencyExchange;
			_logger = logger;
		}

		public Task<string> ConvertTableNameInCaseOfPrimaryCurrency(string tableName)
		{
			Convertions.TryGetValue(tableName, out var info);
			return Task.FromResult(info?.SourceTable ?? tableName);
		}

		public async Task<List<Dictionary<string, object>>> ConvertTableToPrimaryCurrencyTable(List<Dictionary<string, object>> data, string tableName, Currency targetCurrency)
		{
			if (!Convertions.TryGetValue(tableName, out var info))
			{
				// No convertion needed
				return data;
			}

			// Preload exchange rates for better performance
			await _currencyExchange.PreloadAllExchangeRates();

			var convertedData = new List<Dictionary<string, object>>();

			foreach (var record in data)
			{
				var convertedRecord = await info.Convert(record, _currencyExchange, targetCurrency);
				convertedData.Add(convertedRecord);
			}

			return convertedData;
		}

		private record ConvertionInfo(string SourceTable, Func<Dictionary<string, object>, ICurrencyExchange, Currency, Task<Dictionary<string, object>>> Convert);
	}

}