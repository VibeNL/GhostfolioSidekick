using GhostfolioSidekick.Model.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class MarketDataTypeConfiguration : IEntityTypeConfiguration<MarketData>
	{
		public void Configure(EntityTypeBuilder<MarketData> builder)
		{
			builder.ToTable("MarketData");

			builder.Property<int>("ID")
				.HasColumnType("integer")
				.ValueGeneratedOnAdd()
				.HasAnnotation("Key", 0);

			// Configure shadow properties for foreign key
			builder.Property<string>("SymbolProfileSymbol").IsRequired();
			builder.Property<string>("SymbolProfileDataSource").IsRequired();

			// Configure currency
			builder.ComplexProperty(x => x.Currency).Property(x => x.Symbol).HasColumnName("Currency");
		
			builder.Property(b => b.TradingVolume).HasColumnName("TradingVolume");
			builder.Property(b => b.Date).HasColumnName("Date");

			// Add Unique index on Symbol, Date and Source
			builder.HasIndex("SymbolProfileDataSource", "SymbolProfileSymbol", nameof(MarketData.Date))
				.IsUnique()
				.HasDatabaseName("IX_MarketData_SymbolProfileDataSource_SymbolProfileSymbol_Date");
		}
	}
}
