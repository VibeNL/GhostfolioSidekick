using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;

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
			using (PdfDocument document = PdfDocument.Open(filename))
			{
				for (var i = 0; i < document.NumberOfPages; i++)
				{
					var page = document.GetPage(i + 1);
					activities.AddRange(ParseWords(page));
				}
			}

			return activities;
		}

		private List<PartialActivity> ParseWords(Page page)
		{
			var activities = new List<PartialActivity>();

			// detect headers
			var headersAndTopLeft = new Dictionary<string, Point>();

			bool inHeader = false;
			List<Word> words = page.GetWords().ToList();
			for (int i = 0; i < words.Count; i++)
			{
				var word = words[i];

				if (headersAndTopLeft.Count == TableKeyWords.Count) // parsing rows
				{
					throw new NotSupportedException();
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
							if (words[i+j].Text != keyword)
							{
								keywordMatch = false;
								break;
							}
						}

						if (keywordMatch)
						{
							headersAndTopLeft.Add(kw, ToPoint(word.BoundingBox.BottomLeft));
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
						headersAndTopLeft.Clear();
					}
				}

				if (Keyword_Saldo == word.Text) // end of header
				{
					inHeader = false;
				}
			}

			return activities;
		}

		private static Point ToPoint(PdfPoint p)
		{
			return new Point(p.X, p.Y);
		}
	}
}
