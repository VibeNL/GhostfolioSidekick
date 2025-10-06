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

			builder.ComplexProperty(b => b.Money).Property(x => x.Amount).HasColumnName("Amount");
			builder.ComplexProperty(b => b.Money).ComplexProperty(x => x.Currency).Property(x => x.Symbol).HasColumnName("Currency");

			builder.HasIndex(x => new { x.AccountId, x.Date }).IsUnique();
		}
	}
}
