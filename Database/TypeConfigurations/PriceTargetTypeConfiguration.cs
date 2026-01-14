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
			builder.Property(x => x.HighestTargetPrice).IsRequired();
			builder.Property(x => x.AverageTargetPrice).IsRequired();
			builder.Property(x => x.LowestTargetPrice).IsRequired();
			builder.Property(x => x.Rating).IsRequired();
			builder.Property(x => x.NumberOfBuys).IsRequired();
			builder.Property(x => x.NumberOfHolds).IsRequired();
			builder.Property(x => x.NumberOfSells).IsRequired();
		}
	}
}
