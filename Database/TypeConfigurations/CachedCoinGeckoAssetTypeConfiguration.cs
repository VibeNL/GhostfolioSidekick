using GhostfolioSidekick.Database.Caches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class CachedCoinGeckoAssetTypeConfiguration : IEntityTypeConfiguration<CachedCoinGeckoAsset>
	{
		public void Configure(EntityTypeBuilder<CachedCoinGeckoAsset> builder)
		{
			builder.ToTable("CachedCoinGeckoAsset");
			builder.HasKey(a => a.Id);
		}
	}
}
