//namespace GhostfolioSidekick.Ghostfolio.API.Mapper
//{
//	internal class SymbolMapper
//	{
//		private readonly List<Mapping> mappings;

//		public SymbolMapper(IEnumerable<Mapping> mappings)
//		{
//			this.mappings = mappings.ToList();
//		}

//		internal string MapCurrency(string sourceCurrency)
//		{
//			return Map(MappingType.Currency, sourceCurrency);
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

//	internal enum TypeOfMapping
//	{
//		CURRENCY,

//		IDENTIFIER
//	}
//}
