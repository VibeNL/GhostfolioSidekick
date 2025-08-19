using GhostfolioSidekick.Model.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class AccountTypeConfiguration : IEntityTypeConfiguration<Account>
	{
		public void Configure(EntityTypeBuilder<Account> builder)
		{
			builder.ToTable("Accounts");
			builder.HasKey(a => a.Id);

			builder.Property(a => a.SyncActivities)
				.HasDefaultValue(true);

			builder.Property(a => a.SyncBalance)
				.HasDefaultValue(true);

			builder.HasMany(a => a.Balance)
				.WithOne()
				.HasForeignKey(b => b.AccountId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}
