using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class ActivityTypeConfiguration : IEntityTypeConfiguration<Activity>
	{
		public void Configure(EntityTypeBuilder<Activity> builder)
		{
			builder.ToTable("Activities");
			builder.HasDiscriminator<string>("Type")
					.HasValue<BuySellActivity>("1")
					.HasValue<CashDepositWithdrawalActivity>("2");
		}
	}
}
