//using GhostfolioSidekick.Model;
//using GhostfolioSidekick.Model.Accounts;
//using GhostfolioSidekick.Model.Activities;
//using GhostfolioSidekick.Model.Activities.Types;
//using GhostfolioSidekick.Model.Market;
//using GhostfolioSidekick.Model.Symbols;

//namespace GhostfolioSidekick.GhostfolioAPI.API.Mapper
//{
//	public static class ContractToModelMapper
//	{
//		public const string DataSourcePrefix = "GHOSTFOLIO_";

//		public static SymbolProfile MapSymbolProfile(Contract.SymbolProfile symbolProfile)
//		{
//			var symbol = new SymbolProfile(
//				symbolProfile.Symbol,
//				symbolProfile.Name,
//				MapIdentifiers(symbolProfile),
//				new Currency(symbolProfile.Currency!),
//				DataSourcePrefix + symbolProfile.DataSource,
//				EnumMapper.ParseAssetClass(symbolProfile.AssetClass),
//				EnumMapper.ParseAssetSubClass(symbolProfile.AssetSubClass),
//				MapCountries(symbolProfile.Countries),
//				MapSectors(symbolProfile.Sectors))
//			{
//				Comment = symbolProfile.Comment,
//				ISIN = symbolProfile.ISIN,
//			};

//			return symbol;
//		}

//		private static List<string> MapIdentifiers(Contract.SymbolProfile symbolProfile)
//		{
//			List<string> value = [symbolProfile.ISIN, symbolProfile.Symbol];
//			value = value.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
//			return value;
//		}

//		private static SectorWeight[] MapSectors(Contract.Sector[] sectors)
//		{
//			return (sectors ?? []).Select(x => new SectorWeight(x.Name, x.Weight)).ToArray();
//		}

//		private static CountryWeight[] MapCountries(Contract.Country[] countries)
//		{
//			return (countries ?? []).Select(x => new CountryWeight(x.Name, x.Code, x.Continent, x.Weight)).ToArray();
//		}

//		internal static IEnumerable<Activity> MapToActivities(object accounts, Contract.Activity[] existingActivities)
//		{
//			throw new NotImplementedException();
//		}
//	}
//}
