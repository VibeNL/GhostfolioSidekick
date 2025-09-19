using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using System.Text.Json;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Services
{
	public interface IServerCurrencyConversion
	{
		Task<List<Dictionary<string, object>>> ConvertCurrencyFields(List<Dictionary<string, object>> data, string tableName, Currency targetCurrency);
	}

	public class ServerCurrencyConversion : IServerCurrencyConversion
	{
		private readonly ICurrencyExchange _currencyExchange;
		private readonly ILogger<ServerCurrencyConversion> _logger;

		// Define which tables have currency fields that need conversion
		private readonly Dictionary<string, List<CurrencyFieldDefinition>> _currencyFields = new()
		{
			{
				"CalculatedSnapshots", new List<CurrencyFieldDefinition>
				{
					new("TotalValue_Amount", "TotalValue_Currency", "Date"),
					new("CurrentUnitPrice_Amount", "CurrentUnitPrice_Currency", "Date"),
					new("TotalInvested_Amount", "TotalInvested_Currency", "Date")
				}
			},
			{
				"Balances", new List<CurrencyFieldDefinition>
				{
					new("Money_Amount", "Money_Currency", "Date")
				}
			}
		};

		public ServerCurrencyConversion(ICurrencyExchange currencyExchange, ILogger<ServerCurrencyConversion> logger)
		{
			_currencyExchange = currencyExchange;
			_logger = logger;
		}

		public async Task<List<Dictionary<string, object>>> ConvertCurrencyFields(List<Dictionary<string, object>> data, string tableName, Currency targetCurrency)
		{
			if (!_currencyFields.TryGetValue(tableName, out var fields))
			{
				// No currency fields to convert for this table
				return data;
			}

			// Preload exchange rates for better performance
			await _currencyExchange.PreloadAllExchangeRates();

			var convertedData = new List<Dictionary<string, object>>();

			foreach (var record in data)
			{
				var convertedRecord = new Dictionary<string, object>(record);

				foreach (var field in fields)
				{
					await ConvertMoneyField(convertedRecord, field, targetCurrency);
				}

				convertedData.Add(convertedRecord);
			}

			return convertedData;
		}

		private async Task ConvertMoneyField(Dictionary<string, object> record, CurrencyFieldDefinition field, Currency targetCurrency)
		{
			try
			{
				// Extract the amount, currency, and date values
				if (!record.TryGetValue(field.AmountField, out var amountObj) ||
					!record.TryGetValue(field.CurrencyField, out var currencyObj) ||
					!record.TryGetValue(field.DateField, out var dateObj))
				{
					return; // Skip if any required field is missing
				}

				// Convert values to appropriate types
				if (!decimal.TryParse(amountObj?.ToString(), out var amount) ||
					string.IsNullOrEmpty(currencyObj?.ToString()) ||
					!DateOnly.TryParse(dateObj?.ToString(), out var date))
				{
					return; // Skip if conversion fails
				}

				var sourceCurrency = Currency.GetCurrency(currencyObj.ToString()!);

				// Skip conversion if already in target currency
				if (sourceCurrency == targetCurrency)
				{
					return;
				}

				// Convert the money
				var sourceMoney = new Money(sourceCurrency, amount);
				var convertedMoney = await _currencyExchange.ConvertMoney(sourceMoney, targetCurrency, date);

				// Update the record with converted values
				record[field.AmountField] = convertedMoney.Amount;
				record[field.CurrencyField] = targetCurrency.Symbol;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to convert currency field {AmountField} in record", field.AmountField);
				// Continue without conversion on error
			}
		}

		private record CurrencyFieldDefinition(string AmountField, string CurrencyField, string DateField);
	}
}