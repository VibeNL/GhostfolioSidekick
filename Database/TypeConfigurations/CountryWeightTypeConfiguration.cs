using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class CountryWeightTypeConfiguration : IEntityTypeConfiguration<CountryWeight>
	{
		public void Configure(EntityTypeBuilder<CountryWeight> builder)
		{
			builder.ToTable("CountryWeights");
			builder.HasKey(x => x.Code);
		}
	}
}
