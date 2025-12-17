using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.GoldRepublic
{
	public partial class GoldRepublicParser(IPdfToWordsParser parsePDfToWords) : PdfBaseParser(parsePDfToWords)
	{
		protected override bool CanParseRecords(List<SingleWordToken> words)
		{
			try
			{
				bool hasGoldRepublic = ContainsSequence(new[] { "WWW.GOLDREPUBLIC.COM" }, words);
				bool hasAccountStatement = ContainsSequence(new[] { "Account", "Statement" }, words);

				if (hasGoldRepublic && hasAccountStatement)
				{
					return true;
				}

				return false;
			}
			catch
			{
				return false;
			}
		}

		protected override List<PartialActivity> ParseRecords(List<SingleWordToken> words)
		{
			throw new NotSupportedException();
		}
	}
		
}
