using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
    public class UpcomingDividendTypeConfiguration : IEntityTypeConfiguration<UpcomingDividend>
    {
        public void Configure(EntityTypeBuilder<UpcomingDividend> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Symbol).IsRequired();
            builder.Property(x => x.ExDividendDate).IsRequired();
            builder.Property(x => x.PaymentDate).IsRequired();

			builder.ComplexProperty(x => x.Amount).IsRequired().Property(x => x.Amount).HasColumnName(nameof(UpcomingDividend.Amount));
			builder.ComplexProperty(x => x.Amount).IsRequired().ComplexProperty(x => x.Currency).Property(x => x.Symbol).HasColumnName("Currency" + nameof(UpcomingDividend.Amount));
		}
    }
}
