using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class CurrencyExchangeProfileTypeConfiguration : IEntityTypeConfiguration<CurrencyExchangeProfile>
	{
		public void Configure(EntityTypeBuilder<CurrencyExchangeProfile> builder)
		{
			builder.ToTable("CurrencyExchangeProfile");
			
			builder.HasKey(x => x.ID);
			builder.HasMany(x => x.Rates).WithOne().OnDelete(DeleteBehavior.Cascade);

			// Configure Currency properties as complex properties correctly
			builder.Property(x => x.SourceCurrency)
				.HasConversion(
					currency => currency.Symbol,
					symbol => Currency.GetCurrency(symbol))
				.HasColumnName("SourceCurrency");

			builder.Property(x => x.TargetCurrency)
				.HasConversion(
					currency => currency.Symbol,
					symbol => Currency.GetCurrency(symbol))
				.HasColumnName("TargetCurrency");

			builder.HasIndex("SourceCurrency", "TargetCurrency").IsUnique();
		}
	}
}
