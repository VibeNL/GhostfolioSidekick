//using GhostfolioSidekick.Configuration;
//using GhostfolioSidekick.Model;

//namespace GhostfolioSidekick.GhostfolioAPI.API.Mapper
//{
//	public class SymbolMapper : ICurrencyMapper
//	{
//		private readonly List<Mapping> mappings;

//		public SymbolMapper(IEnumerable<Mapping> mappings)
//		{
//			this.mappings = mappings.ToList();
//		}

//		public Currency Map(string currency)
//		{
//			return new Currency(Map(MappingType.Currency, currency));
//		}

//		internal string MapSymbol(string identifier)
//		{
//			return Map(MappingType.Symbol, identifier);
//		}

//		private string Map(MappingType type, string sourceCurrency)
//		{
//			return mappings.SingleOrDefault(x => x.MappingType == type && x.Source == sourceCurrency)?.Target ?? sourceCurrency;
//		}
//	}
//}
