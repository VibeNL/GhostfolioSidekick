using GhostfolioSidekick.Database.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class StockSplitTypeConfiguration : IEntityTypeConfiguration<StockSplit>
	{
		public void Configure(EntityTypeBuilder<StockSplit> builder)
		{
			builder.HasIndex(x => new { x.SymbolProfileId, x.Date }).IsUnique();
		}
	}
}
