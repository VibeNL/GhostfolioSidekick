using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using GhostfolioSidekick.Database.Model;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class StockSplitListTypeConfiguration : IEntityTypeConfiguration<StockSplitList>
	{
		public void Configure(EntityTypeBuilder<StockSplitList> builder)
		{
			builder.HasOne(x => x.SymbolProfile)
				.WithOne(x => x.StockSplitList)
				.HasForeignKey<StockSplitList>(x => x.SymbolProfileId);
		}
	}
}
