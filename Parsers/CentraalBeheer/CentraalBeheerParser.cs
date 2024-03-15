using GhostfolioSidekick.Model;
using GhostfolioSidekick.Parsers.PDFParser;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.CentraalBeheer
{
	public class CentraalBeheerParser : PDFBaseImporter<CentraalBeheerRecord>
	{
		private readonly ICurrencyMapper currencyMapper;
		private readonly CultureInfo cultureInfo = new("nl-NL");
		public CentraalBeheerParser(ICurrencyMapper currencyMapper)
		{
			this.currencyMapper = currencyMapper;
		}

		protected override IEnumerable<CentraalBeheerRecord> ParseTokens(List<Token> tokens)
		{
			var records = new List<CentraalBeheerRecord>();
			for (int i = 0; i < tokens.Count; i++)
			{
				Token? token = tokens[i];
				switch (token)
				{
					case var t when t.Text.Contains("Aankoop"):
						var numberOfTokens = 23;
						var relevantTokens = tokens.GetRange(i, numberOfTokens);
						i += numberOfTokens;
						records.Add(CreateAankoopRecord(relevantTokens));
						break;
					case var t when t.Text.Contains("Verkoop"):

						break;
					case var t when t.Text.Contains("Overboeking"):

						break;
					default:
						break;
				}
			}

			return records;
		}

		private CentraalBeheerRecord CreateAankoopRecord(List<Token> relevantTokens)
		{
			return new CentraalBeheerRecord()
			{
				Type = relevantTokens[0].Text,
				Date = GetDate(relevantTokens[7].Text, relevantTokens[8].Text, relevantTokens[9].Text),

			};
		}

		private DateTime GetDate(params string[] date)
		{
			if (!DateTime.TryParse(string.Join(" ", date), cultureInfo, DateTimeStyles.None, out DateTime parsedDate))
			{
				throw new ArgumentException("Invalid date format");
			}

			return parsedDate;
		}

		private Money GetMoney(string currencySymbol, string amount)
		{
			if (!decimal.TryParse(amount, cultureInfo, out decimal parsedAmount))
			{
				throw new ArgumentException("Invalid amount format");
			}

			return new Money(new Currency(CurrencyTools.GetCurrencyFromSymbol(currencySymbol)), parsedAmount);
		}

		public static class CurrencyTools
		{
			private static IDictionary<string, string> map;
			static CurrencyTools()
			{
				map = CultureInfo
					.GetCultures(CultureTypes.AllCultures)
					.Where(c => !c.IsNeutralCulture)
					.Select(culture =>
					{
						try
						{
							return new RegionInfo(culture.Name);
						}
						catch
						{
							return null;
						}
					})
					.Where(ri => ri != null)
					.GroupBy(ri => ri!.ISOCurrencySymbol)
					.ToDictionary(x => x.Key, x => x.First()!.CurrencySymbol);
			}

			public static bool TryGetCurrencySymbol(
								  string ISOCurrencySymbol,
								  out string? symbol)
			{
				return map.TryGetValue(ISOCurrencySymbol, out symbol);
			}

			public static string GetCurrencyFromSymbol(string currencySymbol)
			{
				var isoCurrencySymbol = map
					.Where(kvp => kvp.Value == currencySymbol)
					.Select(kvp => kvp.Key)
					.FirstOrDefault();

				if (isoCurrencySymbol == null)
				{
					throw new ArgumentException("Currency symbol not found");
				}

				return isoCurrencySymbol;
			}
		}
	}
}
