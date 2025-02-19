using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.ExternalDataProvider.Manual
{
	public class ManualSymbolMatcher(DatabaseContext databaseContext) : ISymbolMatcher
	{
		public string DataSource => Datasource.MANUAL;

        public async Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] symbolIdentifiers)
        {
            foreach (var identifier in symbolIdentifiers)
            {
				var symbolProfileQuery = databaseContext.SymbolProfiles
						.Where(x => x.DataSource == Datasource.MANUAL);

				var symbolProfile = (await symbolProfileQuery
						.Where(x => identifier.AllowedAssetClasses == null || !identifier.AllowedAssetClasses!.Any() || identifier.AllowedAssetClasses!.Contains(x.AssetClass))
						.Where(x => identifier.AllowedAssetSubClasses == null || x.AssetSubClass == null || !identifier.AllowedAssetSubClasses!.Any() || identifier.AllowedAssetSubClasses!.Contains(x.AssetSubClass.Value))
						.ToListAsync()) // SQLlite does not support string operations that well
						.FirstOrDefault(x => 
							string.Equals(x.Symbol, identifier.Identifier, StringComparison.InvariantCultureIgnoreCase) ||
							string.Equals(x.ISIN, identifier.Identifier, StringComparison.InvariantCultureIgnoreCase) ||
							string.Equals(x.Name, identifier.Identifier, StringComparison.InvariantCultureIgnoreCase) ||
							x.Identifiers.Any(y => string.Equals(y, identifier.Identifier, StringComparison.InvariantCultureIgnoreCase)));
                if (symbolProfile != null)
                {
                    return symbolProfile;
                }
            }

            return null;
        }
	}
}
