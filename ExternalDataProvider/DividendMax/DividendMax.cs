using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Model; // For Money and Currency
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HtmlAgilityPack;

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
    public class DividendMax(HttpClient httpClient) : IUpcomingDividendRepository
    {
		public async Task<IList<UpcomingDividend>> Gather(SymbolProfile symbol)
        {
            var result = new List<UpcomingDividend>();
			if (string.IsNullOrWhiteSpace(symbol?.Symbol))
			{
				return result;
			}

            // Step 1: Get suggest.json
            var suggestUrl = $"https://www.dividendmax.com/suggest.json?q={symbol.Symbol}";
            var suggestResponse = await httpClient.GetFromJsonAsync<List<SuggestResult>>(suggestUrl);
            if (suggestResponse == null || suggestResponse.Count == 0) return result;

            // Step 2: Get sub-url
            var subUrl = suggestResponse[0].Path;
            if (string.IsNullOrWhiteSpace(subUrl)) return result;
            var pageUrl = $"https://www.dividendmax.com{subUrl}";
            var html = await httpClient.GetStringAsync(pageUrl);

            // Step 3: Parse table
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'mdc-data-table__table')]");
            if (table == null) return result;
            var rows = table.SelectNodes(".//tbody/tr");
            if (rows == null) return result;

            int id = 1;
            foreach (var row in rows)
            {
                var dividend = ParseDividendRow(row, symbol.Symbol, id);
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
            if (cells == null || cells.Count < 9) return null;

            var exDivDateStr = cells[3].InnerText.Trim();
            var payDateStr = cells[4].InnerText.Trim();
            var declAmountStr = cells[7].InnerText.Trim();
            var currencyStr = cells[5].InnerText.Trim();

            if (string.IsNullOrWhiteSpace(declAmountStr) || declAmountStr == "-" || declAmountStr == "&mdash;") return null;
            if (!DateTime.TryParse(exDivDateStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var exDivDate)) return null;
            if (exDivDate <= DateTime.Today) return null;

            DateOnly exDivDateOnly = DateOnly.FromDateTime(exDivDate);
            DateOnly payDateOnly = DateOnly.FromDateTime(DateTime.TryParse(payDateStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var payDate) ? payDate : exDivDate);
            decimal amount = decimal.TryParse(declAmountStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amt) ? amt : 0;
            var currency = Currency.GetCurrency(currencyStr);

            // Guess type/state from text (simple mapping)
            var type = DividendType.Cash;
            var state = DividendState.Declared;

            return new UpcomingDividend
            {
                Id = id,
                Symbol = symbol,
                ExDividendDate = exDivDateOnly,
                PaymentDate = payDateOnly,
                DividendType = type,
                DividendState = state,
                Amount = new Money(currency, amount)
            };
        }

        private sealed class SuggestResult
        {
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3459:Unassigned members should be removed", Justification = "Serializing")]
			public required string Path { get; set; }
        }
    }
}
