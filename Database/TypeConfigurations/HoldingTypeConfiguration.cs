using GhostfolioSidekick.Model.Activities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class HoldingTypeConfiguration : IEntityTypeConfiguration<Holding>
	{
		public void Configure(EntityTypeBuilder<Holding> builder)
		{
			builder.ToTable("Holdings");
			builder.HasMany(h => h.Activities).WithOne().IsRequired().OnDelete(DeleteBehavior.Cascade);
		}
	}
}
