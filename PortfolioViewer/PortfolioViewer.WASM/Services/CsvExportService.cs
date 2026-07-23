using System.Globalization;
using System.Reflection;
using System.Text;
using GhostfolioSidekick.Model;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	/// <summary>
	/// Service for exporting data to CSV and triggering browser download.
	/// </summary>
	public class CsvExportService : ICsvExportService
	{
		private readonly IJSRuntime _jsRuntime;
		private readonly ILogger<CsvExportService> _logger;

		public CsvExportService(IJSRuntime jsRuntime, ILogger<CsvExportService> logger)
		{
			_jsRuntime = jsRuntime;
			_logger = logger;
		}

		public string ExportToCsvString<T>(IEnumerable<T> data, IEnumerable<string>? headers = null)
		{
			if (data == null)
			{
				return string.Empty;
			}

			var dataList = data.ToList();
			if (dataList.Count == 0)
			{
				return string.Empty;
			}

			var type = typeof(T);
			var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(p => p.CanRead)
				.ToList();

			var headerList = headers?.ToList();
			headerList ??= properties.Select(p => FormatHeader(p.Name)).ToList();

			var sb = new StringBuilder();

			// Write header row
			sb.AppendLine(string.Join(",", headerList.Select(FormatCsvValue)));

			// Write data rows
			foreach (var item in dataList)
			{
				var values = properties.Select(p =>
				{
					try
					{
						var value = p.GetValue(item);
						return FormatCellValue(value);
					}
					catch
					{
						// Property getter may throw (e.g., computed properties with currency mismatch)
						return "\"\"";
					}
				});
				sb.AppendLine(string.Join(",", values));
			}

			return sb.ToString();
		}

		public async Task ExportToCsvAsync<T>(IEnumerable<T> data, string fileName, IEnumerable<string>? headers = null)
		{
			try
			{
				var csvContent = ExportToCsvString(data, headers);

				if (string.IsNullOrEmpty(csvContent))
				{
					return;
				}

				var fileNameWithExtension = EnsureCsvExtension(fileName);

				// Use JavaScript interop to trigger browser download
				await _jsRuntime.InvokeVoidAsync("downloadCsv", fileNameWithExtension, csvContent);
			}
			catch (Exception ex)
			{
				// Swallow export errors (JS interop failures, CSV generation issues)
				// to avoid showing Blazor error UI to users, but log for diagnostics
				_logger.LogError(ex, "Failed to export data to CSV file '{FileName}'", fileName);
			}
		}

		private static string FormatHeader(string? name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return string.Empty;
			}

			return name switch
			{
				"GainLoss" => "Gain/Loss",
				"GainLossPercentage" => "Gain/Loss %",
				"UnitPrice" => "Unit Price",
				"DividendPerShare" => "Dividend Per Share",
				"ExDate" => "Ex-Date",
				"PaymentDate" => "Payment Date",
				"TransactionId" => "Transaction ID",
				"ActivityType" => "Activity Type",
				"SymbolIdentifiers" => "Symbol Identifiers",
				_ => name
			};
		}

		private static string FormatCsvValue(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return "\"\"";
			}

			// If value contains comma, quote, or newline, wrap in quotes and escape internal quotes
			if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
			{
				return "\"" + value.Replace("\"", "\"\"") + "\"";
			}

			return value;
		}

		private static string FormatCellValue(object? value)
		{
			if (value == null)
			{
				return "\"\"";
			}

			// Handle Money type
			if (value is Money money)
			{
				var amountStr = money.Amount.ToString(CultureInfo.InvariantCulture);
				return FormatCsvValue($"{amountStr} {money.Currency}");
			}

			// Handle DateTime
			if (value is DateTime dt)
			{
				return FormatCsvValue(dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
			}

			// Handle DateOnly
			if (value.GetType().Name == "DateOnly" || value.GetType().FullName?.Contains("DateOnly") == true)
			{
				var dateProp = value.GetType().GetProperty("Year");
				if (dateProp != null)
				{
					var year = (int)dateProp.GetValue(value)!;
					var month = (int)value.GetType().GetProperty("Month")!.GetValue(value)!;
					var day = (int)value.GetType().GetProperty("Day")!.GetValue(value)!;
					var dateStr = $"{year:D4}-{month:D2}-{day:D2}";
					return FormatCsvValue(dateStr);
				}
				return FormatCsvValue(value.ToString() ?? "");
			}

			// Handle enumerable (lists, arrays)
			if (value is IEnumerable<object> enumerable && !(value is string))
			{
				return FormatCsvValue(string.Join("; ", enumerable.Select(x => x?.ToString() ?? "")));
			}

			if (value is IEnumerable<string> stringEnumerable)
			{
				return FormatCsvValue(string.Join("; ", stringEnumerable));
			}

			// Handle decimal with invariant culture to avoid comma as decimal separator
			if (value is decimal dec)
			{
				return FormatCsvValue(dec.ToString(CultureInfo.InvariantCulture));
			}

			// Handle int with invariant culture for consistency
			if (value is int i)
			{
				return FormatCsvValue(i.ToString(CultureInfo.InvariantCulture));
			}

			// Default: convert to string
			return FormatCsvValue(value.ToString() ?? "");
		}

		private static string EnsureCsvExtension(string fileName)
		{
			if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
			{
				return fileName;
			}
			return fileName + ".csv";
		}
	}
}
