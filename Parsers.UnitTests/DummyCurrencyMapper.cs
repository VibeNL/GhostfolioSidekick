using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Parsers.UnitTests
{
	internal class DummyCurrencyMapper : ICurrencyMapper
	{
		public static DummyCurrencyMapper Instance { get; } = new DummyCurrencyMapper();

		public Currency Map(string currency)
		{
			return Currency.GetCurrency(currency);
		}
	}
}
