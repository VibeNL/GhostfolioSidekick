using GhostfolioSidekick.Model.Performance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	public class UpcomingDividendsSnapshotTypeConfiguration : IEntityTypeConfiguration<UpcomingDividendsSnapshot>
	{
		public void Configure(EntityTypeBuilder<UpcomingDividendsSnapshot> builder)
		{
			builder.HasKey(x => x.Id);
			builder.Property(x => x.HoldingId).IsRequired();
			builder.Property(x => x.CalculationDate).IsRequired();
			builder.Property(x => x.TotalExpectedReturn).HasPrecision(18, 8).IsRequired();
			builder.Property(x => x.TotalExpectedReturnPrimary).HasPrecision(18, 8).IsRequired();
			builder.OwnsOne(x => x.Currency, cb =>
			{
				cb.Property(c => c.Symbol).HasColumnName("CurrencySymbol").IsRequired();
			});
		}
	}
}
