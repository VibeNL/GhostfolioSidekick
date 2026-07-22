using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	/// <summary>
	/// Service for exporting data to CSV and triggering browser download.
	/// </summary>
	public class CsvExportService : ICsvExportService
	{
		private readonly IJSRuntime _jsRuntime;

		public CsvExportService(IJSRuntime jsRuntime)
		{
			_jsRuntime = jsRuntime;
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
					var value = p.GetValue(item);
					return FormatCellValue(value);
				});
				sb.AppendLine(string.Join(",", values));
			}

			return sb.ToString();
		}

		public async Task ExportToCsvAsync<T>(IEnumerable<T> data, string fileName, IEnumerable<string>? headers = null)
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

			// Handle Money types
			if (value.GetType().Name == "Money" || value.GetType().FullName?.Contains("Money") == true)
			{
				var amountProp = value.GetType().GetProperty("Amount");
				if (amountProp != null)
				{
					var amount = amountProp.GetValue(value);
					var amountStr = amount is decimal d ? d.ToString(CultureInfo.InvariantCulture) : amount?.ToString() ?? "";
					var currencyProp = value.GetType().GetProperty("Currency");
					var currency = currencyProp?.GetValue(value)?.ToString() ?? "";
					return FormatCsvValue($"{amountStr} {currency}");
				}
				return FormatCsvValue(value.ToString() ?? "");
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
