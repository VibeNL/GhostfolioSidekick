using GhostfolioSidekick.Model;
using GhostfolioSidekick.Parsers.PDFParser;

namespace GhostfolioSidekick.Parsers.CentraalBeheer
{
	public class CentraalBeheerParser : PDFBaseImporter<CentraalBeheerRecord>
	{
		private readonly ICurrencyMapper currencyMapper;

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
						var relevantTokens = tokens.GetRange(i, 30);
						i += 30;
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
			throw new NotImplementedException();
		}
	}
}
