using GhostfolioSidekick.Model.Performance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	public class UpcomingDividendTimelineEntryTypeConfiguration : IEntityTypeConfiguration<UpcomingDividendTimelineEntry>
	{
		public void Configure(EntityTypeBuilder<UpcomingDividendTimelineEntry> builder)
		{
			builder.HasKey(x => x.Id);
			builder.Property(x => x.HoldingId).IsRequired();
			builder.Property(x => x.ExpectedDate).IsRequired();
			builder.Property(x => x.Amount).HasPrecision(18, 8).IsRequired();
			builder.Property(x => x.AmountPrimaryCurrency).HasPrecision(18, 8).IsRequired();
            builder.OwnsOne(x => x.Currency, cb =>
			{
				cb.Property(c => c.Symbol).HasColumnName("CurrencySymbol").IsRequired();
			});
			builder.Property(x => x.DividendType).IsRequired();
			builder.Property(x => x.DividendState).IsRequired();
		}
	}
}
