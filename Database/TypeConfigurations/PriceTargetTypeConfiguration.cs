using GhostfolioSidekick.Model.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	public class PriceTargetTypeConfiguration : IEntityTypeConfiguration<PriceTarget>
	{
		public void Configure(EntityTypeBuilder<PriceTarget> builder)
		{
			builder.HasKey(x => x.Id);
			builder.Property(x => x.Symbol).IsRequired().HasMaxLength(50);
			builder.HasIndex(x => x.Symbol);

			builder.ComplexProperty(x => x.HighestTargetCurrency).Property(x => x.Symbol).HasColumnName("HighestTargetPrice");
			builder.Property(x => x.HighestTargetPriceAmount).IsRequired();

			builder.ComplexProperty(x => x.AverageTargetCurrency).Property(x => x.Symbol).HasColumnName("AverageTargetPrice");
			builder.Property(x => x.AverageTargetPriceAmount).IsRequired();

			builder.ComplexProperty(x => x.LowestTargetCurrency).Property(x => x.Symbol).HasColumnName("LowestTargetPrice");
			builder.Property(x => x.LowestTargetPriceAmount).IsRequired();

			builder.Property(x => x.Rating).IsRequired();
			builder.Property(x => x.NumberOfBuys).IsRequired();
			builder.Property(x => x.NumberOfHolds).IsRequired();
			builder.Property(x => x.NumberOfSells).IsRequired();
		}
	}
}
