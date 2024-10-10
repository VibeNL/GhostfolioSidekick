using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class SectorWeightTypeConfiguration : IEntityTypeConfiguration<SectorWeight>
	{
		public void Configure(EntityTypeBuilder<SectorWeight> builder)
		{
			builder.ToTable("SectorWeights");
			builder.HasKey(x => x.Name);
		}
	}
}
