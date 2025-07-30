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

			builder.ComplexProperty(b => b.Close).Property(p => p.Amount).HasColumnName("Close");
			builder.ComplexProperty(b => b.Close).ComplexProperty(c => c.Currency).Property(p => p.Symbol).HasColumnName("CurrencyClose");

			builder.ComplexProperty(b => b.Open).Property(p => p.Amount).HasColumnName("Open");
			builder.ComplexProperty(b => b.Open).ComplexProperty(c => c.Currency).Property(p => p.Symbol).HasColumnName("CurrencyOpen");

			builder.ComplexProperty(b => b.High).Property(p => p.Amount).HasColumnName("High");
			builder.ComplexProperty(b => b.High).ComplexProperty(c => c.Currency).Property(p => p.Symbol).HasColumnName("CurrencyHigh");

			builder.ComplexProperty(b => b.Low).Property(p => p.Amount).HasColumnName("Low");
			builder.ComplexProperty(b => b.Low).ComplexProperty(c => c.Currency).Property(p => p.Symbol).HasColumnName("CurrencyLow");

			builder.Property(b => b.TradingVolume).HasColumnName("TradingVolume");
			builder.Property(b => b.Date).HasColumnName("Date");

			// Add Unique index on Symbol, Date and Source
			builder.HasIndex("CurrencyExchangeProfileID", nameof(MarketData.Date))
				.IsUnique()
				.HasDatabaseName("IX_CurrencyExchangeRate_CurrencyExchangeProfileID_Date");
		}
	}
}
