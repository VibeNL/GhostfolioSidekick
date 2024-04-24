using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser;
using Spire.Pdf.Texts;
using Spire.Pdf;
using System.Text;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class TradeRepublicParserNL : PdfBaseParser, IFileImporter
	{
		private const string Keyword_Datum = "DATUM";
		private const string Keyword_Type = "TYPE";
		private const string Keyword_Beschrijving = "BESCHRIJVING";
		private const string Keyword_BedragBij = "BEDRAG BIJ";
		private const string Keyword_BedragAf = "BEDRAF AF";
		private const string Keyword_Saldo = "SALDO";

		private List<string> TableKeyWords
		{
			get
			{
				return [
					Keyword_Datum,
					Keyword_Type,
					Keyword_Beschrijving,
					Keyword_BedragBij,
					Keyword_BedragAf,
					Keyword_Saldo
				];
			}
		}

		protected override List<PartialActivity> ParseRecords(string filename)
		{
			var activities = new List<PartialActivity>();
			using (PdfDocument document = new PdfDocument())
			{
				// Load a PDF file
				document.LoadFromFile(filename);

				foreach (PdfPageBase page in document.Pages)
				{
					PdfTextExtractor textExtractor = new PdfTextExtractor(page);
					var text = textExtractor.ExtractText(new PdfTextExtractOptions());
					activities.AddRange(ParseWords(text));
				}
			}

			return activities;
		}

		private List<PartialActivity> ParseWords(string text)
		{
			var activities = new List<PartialActivity>();

			List<SingleWordToken> words = SplitText(text);

			// detect headers
			var headers = new List<MultiWordToken>();

			bool inHeader = false;

			for (int i = 0; i < words.Count; i++)
			{
				var word = words[i];

				if (headers.Count == TableKeyWords.Count) // parsing rows
				{
					
				}

				if (Keyword_Datum == word.Text) // start of header
				{
					inHeader = true;
				}

				if (inHeader) // add column headers
				{
					var matched = false;
					foreach (var kw in TableKeyWords)
					{
						var keywordMatch = true;
						string[] keywordSplitted = kw.Split(" ");
						for (int j = 0; j < keywordSplitted.Length; j++)
						{
							string? keyword = keywordSplitted[j];
							if (words[i + j].Text != keyword)
							{
								keywordMatch = false;
								break;
							}
						}

						if (keywordMatch)
						{
							headers.Add(new MultiWordToken(kw, word.BoundingBox));
							matched = true;
#pragma warning disable S127 // "for" loop stop conditions should be invariant
							i += keywordSplitted.Length - 1;
#pragma warning restore S127 // "for" loop stop conditions should be invariant
							break;
						}
					}

					if (!matched)
					{
						inHeader = false;
						headers.Clear();
					}
				}

				if (Keyword_Saldo == word.Text) // end of header
				{
					inHeader = false;
				}
			}

			return activities;
		}

		private static List<SingleWordToken> SplitText(string text)
		{
			// for each line
			var lines = text.Split(Environment.NewLine);
			var words = new List<SingleWordToken>();

			for (int i = 0; i < lines.Length; i++)
			{
				var line = lines[i].ToCharArray();
				var word = new StringBuilder();
				for (int j = 0; j < line.Length; j++)
				{
					switch (line[j])
					{
						case ' ':
							if (word.Length > 0)
							{
								words.Add(new SingleWordToken(word.ToString(), new BoundingBox()));
							}

							word.Clear();
							break;
						default:
							word.Append(line[j]);
							break;
					}
				}

				if (word.Length > 0)
				{
					words.Add(new SingleWordToken(word.ToString(), new BoundingBox()));
				}
			}

			return words;
		}
	}
}
