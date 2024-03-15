using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Parsers.Trading212
{
	public class CentraalBeheerParser : PDFBaseImporter
	{
		private readonly ICurrencyMapper currencyMapper;

		public CentraalBeheerParser(ICurrencyMapper currencyMapper)
		{
			this.currencyMapper = currencyMapper;
		}


	}
}
