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
            builder.ComplexProperty(c => c.Currency).Property(p => p.Symbol).HasColumnName("CurrencySymbol");
			builder.Property(x => x.DividendType).IsRequired();
			builder.Property(x => x.DividendState).IsRequired();
		}
	}
}
