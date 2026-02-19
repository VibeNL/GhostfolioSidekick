using GhostfolioSidekick.Model.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class CurrencyExchangeRateTypeConfiguration : IEntityTypeConfiguration<CurrencyExchangeRate>
	{
		public void Configure(EntityTypeBuilder<CurrencyExchangeRate> builder)
		{
			builder.ToTable("CurrencyExchangeRate");

			builder.Property<int>("ID")
				.HasColumnType("integer")
				.ValueGeneratedOnAdd()
				.HasAnnotation("Key", 0);

			builder.ComplexProperty(c => c.Currency).Property(p => p.Symbol).HasColumnName("Currency");

			// Add Unique index on Symbol, Date and Source
			builder.HasIndex("CurrencyExchangeProfileID", nameof(MarketData.Date))
				.IsUnique()
				.HasDatabaseName("IX_CurrencyExchangeRate_CurrencyExchangeProfileID_Date");
		}
	}
}
