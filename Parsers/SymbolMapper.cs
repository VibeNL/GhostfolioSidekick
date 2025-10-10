using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Parsers
{
	public class SymbolMapper(IEnumerable<Mapping> mappings) : ICurrencyMapper
	{
		private readonly List<Mapping> mappings = [.. mappings];

		public Currency Map(string currency)
		{
			return Currency.GetCurrency(Map(MappingType.Currency, currency));
		}

		public string MapSymbol(string identifier)
		{
			return Map(MappingType.Symbol, identifier);
		}

		private string Map(MappingType type, string sourceCurrency)
		{
			return mappings.SingleOrDefault(x => x.MappingType == type && x.Source == sourceCurrency)?.Target ?? sourceCurrency;
		}
	}
}
