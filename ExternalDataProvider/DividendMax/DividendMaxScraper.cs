using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HtmlAgilityPack;
using GhostfolioSidekick.Utilities;
using GhostfolioSidekick.Model.Activities;

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
	public class DividendMaxScraper(HttpClient httpClient) : IUpcomingDividendRepository
	{
		private const string TableSelector = "//table[contains(@class, 'mdc-data-table__table')]";
		private const string TableRowsSelector = ".//tbody/tr";

		public async Task<IList<UpcomingDividend>> Gather(SymbolProfile symbol)
		{
			if (symbol == null || symbol.WebsiteUrl == null || symbol.DataSource != "DividendMax")
			{
				return [];
			}

			var page = await GetDividendPageHtml(symbol.WebsiteUrl);
			if (string.IsNullOrWhiteSpace(page))
			{
				return [];
			}

			var dividends = ParseDividendsFromHtml(page, symbol.Symbol);
			return dividends;
		}

		private async Task<string?> GetDividendPageHtml(string pageUrl)
		{
			if (string.IsNullOrWhiteSpace(pageUrl))
			{
				return null;
			}

			return await httpClient.GetStringAsync(pageUrl);
		}

		private static IList<UpcomingDividend> ParseDividendsFromHtml(string html, string originalSymbol)
		{
			var result = new List<UpcomingDividend>();

			var doc = new HtmlDocument();
			doc.LoadHtml(html);

			var table = doc.DocumentNode.SelectSingleNode(TableSelector);
			if (table == null)
			{
				return result;
			}

			var rows = table.SelectNodes(TableRowsSelector);
			if (rows == null)
			{
				return result;
			}

			int id = 1;
			foreach (var row in rows)
			{
				var dividend = ParseDividendRow(row, originalSymbol, id);
				if (dividend != null)
				{
					result.Add(dividend);
					id++;
				}
			}

			return result;
		}

		private static UpcomingDividend? ParseDividendRow(HtmlNode row, string symbol, int id)
		{
			var cells = row.SelectNodes("td");
			if (cells == null || cells.Count < 9)
			{
				return null;
			}

			var dividendData = ExtractDividendData(cells);
			if (!IsValidDividendData(dividendData))
			{
				return null;
			}

			var exDivDate = ParseDate(dividendData.ExDivDateStr);
			if (!exDivDate.HasValue || exDivDate.Value <= DateTime.Today)
			{
				return null;
			}

			var payDate = ParseDate(dividendData.PayDateStr) ?? exDivDate.Value;
			var amount = ParseDecimal(dividendData.DeclAmountStr);
			var currency = Currency.GetCurrency(dividendData.CurrencyStr);

			return new UpcomingDividend
			{
				Id = id,
				Symbol = symbol,
				ExDividendDate = DateOnly.FromDateTime(exDivDate.Value),
				PaymentDate = DateOnly.FromDateTime(payDate),
				DividendType = DividendType.Cash,
				DividendState = DividendState.Declared,
				Amount = new Money(currency, amount)
			};
		}

		private static (string ExDivDateStr, string PayDateStr, string DeclAmountStr, string CurrencyStr) ExtractDividendData(HtmlNodeCollection cells)
		{
			return (
				ExDivDateStr: cells[3].InnerText.Trim(),
				PayDateStr: cells[4].InnerText.Trim(),
				DeclAmountStr: cells[7].InnerText.Trim(),
				CurrencyStr: cells[5].InnerText.Trim()
			);
		}

		private static bool IsValidDividendData((string ExDivDateStr, string PayDateStr, string DeclAmountStr, string CurrencyStr) data)
		{
			return !string.IsNullOrWhiteSpace(data.DeclAmountStr) &&
				   data.DeclAmountStr != "-" &&
				   data.DeclAmountStr != "&mdash;";
		}

		private static DateTime? ParseDate(string dateStr)
		{
			return DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;
		}

		private static decimal ParseDecimal(string decimalStr)
		{
			return decimal.TryParse(decimalStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
		}
	}
}
