using GhostfolioSidekick.Model;
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

			builder.OwnsOne<Money>(b => b.Close, m =>
			{
				m.Property(p => p.Amount).HasColumnName("Close");
				m.OwnsOne<Currency>(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName("CurrencyClose");
				});
			});
			builder.OwnsOne<Money>(b => b.Open, m =>
			{
				m.Property(p => p.Amount).HasColumnName("Open");
				m.OwnsOne<Currency>(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName("CurrencyOpen");
				});
			});
			builder.OwnsOne<Money>(b => b.High, m =>
			{
				m.Property(p => p.Amount).HasColumnName("High");
				m.OwnsOne<Currency>(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName("CurrencyHigh");
				});
			});
			builder.OwnsOne<Money>(b => b.Low, m =>
			{
				m.Property(p => p.Amount).HasColumnName("Low");
				m.OwnsOne<Currency>(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName("CurrencyLow");
				});
			});
			builder.Property(b => b.TradingVolume).HasColumnName("TradingVolume");
			builder.Property(b => b.Date).HasColumnName("Date");
		}
	}
}
