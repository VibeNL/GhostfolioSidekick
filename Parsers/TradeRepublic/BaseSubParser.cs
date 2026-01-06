using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public abstract class BaseSubParser : ITradeRepublicActivityParser
	{
		protected abstract TableDefinition[] TableDefinitions { get; }

		protected abstract string[] DateTokens { get; }

		public bool CanParseRecord(string filename, List<SingleWordToken> words)
		{
			return ParseRecords(filename, words).Count != 0; // TODO, pass to the subparsers
		}

		public List<PartialActivity> ParseRecords(string filename, List<SingleWordToken> words)
		{
			var activities = new List<PartialActivity>();

			var rows = PdfTableExtractor.FindTableRowsWithColumns(
				words,
				TableDefinitions);

			var transactionId = $"Trade_Republic_{Path.GetFileName(filename)}";

			foreach (var row in rows.Where(x => x.Columns[0].Any()))
			{
				var parsed = ParseRecord(row, words, transactionId);
				if (parsed != null)
				{
					activities.AddRange(parsed);
				}
			}

			return activities;
		}

		protected abstract IEnumerable<PartialActivity> ParseRecord(PdfTableRowColumns row, List<SingleWordToken> words, string transactionId);

		protected static decimal ParseDecimal(string x)
		{
			// Determine culture based on decimal separator
			var cultureInfo = x.Contains(',') && x.Contains('.')
				? (x.LastIndexOf(',') > x.LastIndexOf('.') 
					? new CultureInfo("de-DE") // European style: 1.234,56
					: new CultureInfo("en-US")) // US style: 1,234.56
				: x.Contains(',') 
					? new CultureInfo("de-DE") // European style: 1234,56
					: new CultureInfo("en-US"); // US style: 1234.56

			if (decimal.TryParse(x, NumberStyles.Currency, cultureInfo, out var result))
			{
				return result;
			}

			throw new FormatException($"Unable to parse '{x}' as decimal.");
		}

		protected static DateTime ParseDateTime(string parseDate)
		{
			parseDate = parseDate
				.Replace('-', '.')
				.Trim('.'); // Just in case

			if (DateTime.TryParseExact(parseDate, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
			{
				dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
				return dateTime;
			}

			if (DateTime.TryParseExact(parseDate, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
			{
				dateOnly = DateTime.SpecifyKind(dateOnly, DateTimeKind.Utc);
				return dateOnly;
			}

			throw new FormatException($"Unable to parse '{parseDate}' as DateTime.");
		}

		protected DateTime DetermineDate(List<SingleWordToken> words)
		{
			// Find the first date token from the language-specific set and take the next token as date
			for (int i = 0; i < words.Count - 1; i++)
			{
				foreach (var dateToken in DateTokens)
				{
					if (words[i].Text.Equals(dateToken, StringComparison.InvariantCultureIgnoreCase))
					{
						return ParseDateTime(words[i + 1].Text);
					}
				}
			}
			
			throw new FormatException($"Unable to determine date from the document. Expected one of: {string.Join(", ", DateTokens)}");
		}

		protected static bool ContainsSequence(string[] wordTexts, string[] pattern)
		{
			for (int i = 0; i <= wordTexts.Length - pattern.Length; i++)
			{
				bool matches = true;
				for (int j = 0; j < pattern.Length; j++)
				{
					if (!wordTexts[i + j].Equals(pattern[j], StringComparison.InvariantCultureIgnoreCase))
					{
						matches = false;
						break;
					}
				}
				
				if (matches)
				{
					return true;
				}
			}
			
			return false;
		}
	}
}
