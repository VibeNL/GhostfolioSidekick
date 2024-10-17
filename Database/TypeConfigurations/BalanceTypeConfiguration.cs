using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class BalanceTypeConfiguration : IEntityTypeConfiguration<Balance>
	{
		public void Configure(EntityTypeBuilder<Balance> builder)
		{
			builder.ToTable("Balances");
			builder.OwnsOne<Money>(b => b.Money, m =>
			{
				m.Property(p => p.Amount).HasColumnName("Amount");
				m.OwnsOne<Currency>(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName("Currency");
				});
			});
		}
	}
}
