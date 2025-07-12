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

			// Configure shadow properties for foreign key
			builder.Property<string>("SymbolProfileSymbol").IsRequired();
			builder.Property<string>("SymbolProfileDataSource").IsRequired();
		}
	}
}
