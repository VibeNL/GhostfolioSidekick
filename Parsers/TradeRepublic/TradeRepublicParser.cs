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
	public class TradeRepublicParser : PdfBaseParser, IFileImporter
	{
		protected override List<PartialActivity> ParseRecords(string filename)
		{
			using (PdfDocument document = PdfDocument.Open(filename))
			{
				var singleWords = new List<SingleWordToken>();

				for (var i = 0; i < document.NumberOfPages; i++)
				{
					Page page = document.GetPage(i + 1);
					foreach (var word in page.GetWords())
					{
						singleWords.Add(new SingleWordToken(word.Text, ToPoint(word.BoundingBox.TopLeft), ToPoint(word.BoundingBox.BottomRight)));
					}
				}
			}

			return [];
		}

		private Point ToPoint(PdfPoint p)
		{
			return new Point(p.X, p.Y);
		}
	}
}
