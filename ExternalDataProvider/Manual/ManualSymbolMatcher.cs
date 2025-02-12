using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.ExternalDataProvider.Manual
{
	public class ManualSymbolMatcher(DatabaseContext databaseContext) : ISymbolMatcher
	{
		public string DataSource => Datasource.MANUAL;

        public async Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] symbolIdentifiers)
        {
            foreach (var identifier in symbolIdentifiers)
            {
                var symbolProfile = await databaseContext.SymbolProfiles
						.Where(x => x.DataSource == Datasource.MANUAL)
						.FirstOrDefaultAsync(x => x.Symbol == identifier.Identifier);
                if (symbolProfile != null)
                {
                    return symbolProfile;
                }
            }

            return null;
        }
	}
}
