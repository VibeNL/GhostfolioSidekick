using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class StockSplitTypeConfiguration : IEntityTypeConfiguration<StockSplit>
	{
		public void Configure(EntityTypeBuilder<StockSplit> builder)
		{
			builder.ToTable("StockSplits");
			builder.Property<int>("ID")
				.HasColumnType("integer")
				.ValueGeneratedOnAdd()
				.HasAnnotation("Key", 0);
		}
	}
}
