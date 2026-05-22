using GhostfolioSidekick.ExternalDataProvider.Cache;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using HtmlAgilityPack;
using System.Globalization;

namespace GhostfolioSidekick.ExternalDataProvider.DividendMax
{
	/// <summary>
	/// Uses DividendMax.com to gather upcoming dividends.
	/// 
	/// Follow these steps:
	/// 1) https://www.dividendmax.com/suggest.json?q={symbol}
	/// 2) follow the URL in path (sub url) to get the website
	/// 3) parse the table (class="mdc-data-table__table") with the following columns:
	///     Status, Type, Decl. date, Ex-div date, Pay date, Decl. Currency, Forecast amount, Decl. amount, Accuracy
	/// 4) generate UpcomingDividend objects from the rows where Ex-div date is in the future. and the decl. amount is not empty / a '-',
	/// </summary>
	public class DividendMaxScraper(HttpClient httpClient, IExternalDataCacheService cacheService) : IDividendRepository
	{
		private const string TableSelector = "//table[contains(@class, 'mdc-data-table__table')]";
		private const string TableRowsSelector = ".//tbody/tr";

		public Task<bool> IsSymbolSupported(SymbolProfile symbol)
		{
			return symbol == null || symbol.WebsiteUrl == null || symbol.DataSource != Datasource.DividendMax
				? Task.FromResult(false)
				: Task.FromResult(true);
		}

		public async Task<IList<Dividend>> GetDividends(SymbolProfile symbol)
		{
			if (!await IsSymbolSupported(symbol))
			{
				return [];
			}

			string cacheKey = $"{symbol.Symbol}";
			return await cacheService.GetOrAddAsync<IList<Dividend>>(Source.DividendMax, TypeOfData.Dividends, cacheKey, async () =>
			{
				string? page = await GetDividendPageHtml(symbol.WebsiteUrl!);
				if (string.IsNullOrWhiteSpace(page))
				{
					return [];
				}

				List<Dividend> dividends = ParseDividendsFromHtml(page);

				// Group per ex-dividend date and sum
				dividends = [.. dividends
				   .GroupBy(d => new { d.ExDividendDate, d.PaymentDate, d.DividendType, d.DividendState })
				   .Select(g => new Dividend
				   {
					   Id = 0,
					   ExDividendDate = g.Key.ExDividendDate,
					   PaymentDate = g.Key.PaymentDate,
					   DividendType = g.Key.DividendType,
					   DividendState = g.Key.DividendState,
					   Amount = new Money(g.First().Amount.Currency, g.Sum(d => d.Amount.Amount))
				   })];

				return dividends;
			}, TimeSpan.FromDays(1)) ?? [];
		}

		private async Task<string?> GetDividendPageHtml(string pageUrl)
		{
			return string.IsNullOrWhiteSpace(pageUrl) ? null : await httpClient.GetStringAsync(pageUrl);
		}

		private static List<Dividend> ParseDividendsFromHtml(string html)
		{
			var result = new List<Dividend>();

			var doc = new HtmlDocument();
			doc.LoadHtml(html);

			HtmlNode table = doc.DocumentNode.SelectSingleNode(TableSelector);
			if (table == null)
			{
				return result;
			}

			HtmlNodeCollection rows = table.SelectNodes(TableRowsSelector);
			if (rows == null)
			{
				return result;
			}

			foreach (HtmlNode row in rows)
			{
				Dividend? dividend = ParseDividendRow(row);
				if (dividend != null)
				{
					result.Add(dividend);
				}
			}

			return result;
		}

		private static Dividend? ParseDividendRow(HtmlNode row)
		{
			HtmlNodeCollection cells = row.SelectNodes("td");
			if (cells == null || cells.Count < 9)
			{
				return null;
			}

			(string ExDivDateStr, string PayDateStr, string DeclAmountStr, string CurrencyStr, string Type) dividendData = ExtractDividendData(cells);
			if (!IsValidDividendData(dividendData))
			{
				return null;
			}

			DateTime? exDivDate = ParseDate(dividendData.ExDivDateStr);
			if (!exDivDate.HasValue)
			{
				return null;
			}

			DateTime payDate = ParseDate(dividendData.PayDateStr) ?? exDivDate.Value;
			decimal amount = ParseDecimal(dividendData.DeclAmountStr);
			var currency = Currency.GetCurrency(dividendData.CurrencyStr);
			DividendType type = ParseType(dividendData.Type);

			return new Dividend
			{
				Id = 0,
				ExDividendDate = DateOnly.FromDateTime(exDivDate.Value),
				PaymentDate = DateOnly.FromDateTime(payDate),
				DividendType = type,
				DividendState = DividendState.Declared,
				Amount = new Money(currency, amount)
			};
		}

		private static DividendType ParseType(string type)
		{
			return type switch
			{
				"Monthly" => DividendType.Cash,
				"Quarterly" => DividendType.Cash,
				"Final" => DividendType.Cash,
				"Interim" => DividendType.CashInterim,
				"Special" => DividendType.SpecialCash,
				_ => throw new NotSupportedException($"Dividend type '{type}' is not supported."),
			};
		}

		private static (string ExDivDateStr, string PayDateStr, string DeclAmountStr, string CurrencyStr, string Type) ExtractDividendData(HtmlNodeCollection cells)
		{
			return (
				ExDivDateStr: cells[3].InnerText.Trim(),
				PayDateStr: cells[4].InnerText.Trim(),
				DeclAmountStr: cells[7].InnerText.Trim(),
				CurrencyStr: cells[5].InnerText.Trim(),
				Type: cells[1].InnerText.Trim()
			);
		}

		private static bool IsValidDividendData((string ExDivDateStr, string PayDateStr, string DeclAmountStr, string CurrencyStr, string Type) data)
		{
			return !string.IsNullOrWhiteSpace(data.DeclAmountStr) &&
				   data.DeclAmountStr != "-" &&
				   data.DeclAmountStr != "&mdash;";
		}

		private static DateTime? ParseDate(string dateStr)
		{
			return DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date) ? date : null;
		}

		// Parse strings like 8500sen, 125¢ and 23.5c
		private static decimal ParseDecimal(string decimalStr)
		{
			// Always take the numeric part, parse it, and divide by 100
			if (string.IsNullOrWhiteSpace(decimalStr))
			{
				return 0;
			}

			string numPart = new([.. decimalStr.Where(c => char.IsDigit(c) || c == '.' || c == ',' || c == '-')]);
			if (string.IsNullOrWhiteSpace(numPart))
			{
				return 0;
			}

			// Replace comma with dot for decimal separator if needed
			numPart = numPart.Replace(',', '.');

			return decimal.TryParse(numPart, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value) ? value / 100m : 0;
		}
	}
}
