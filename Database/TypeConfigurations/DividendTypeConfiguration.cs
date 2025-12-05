using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
    public class DividendTypeConfiguration : IEntityTypeConfiguration<Dividend>
    {
        public void Configure(EntityTypeBuilder<Dividend> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Symbol).IsRequired();
            builder.Property(x => x.ExDividendDate).IsRequired();
            builder.Property(x => x.PaymentDate).IsRequired();
            builder.Property(x => x.DividendType).IsRequired();
            builder.Property(x => x.DividendState).IsRequired();

			builder.ComplexProperty(x => x.Amount).IsRequired().Property(x => x.Amount).HasColumnName(nameof(Dividend.Amount));
			builder.ComplexProperty(x => x.Amount).IsRequired().ComplexProperty(x => x.Currency).Property(x => x.Symbol).HasColumnName("Currency" + nameof(Dividend.Amount));
		}
    }
}
