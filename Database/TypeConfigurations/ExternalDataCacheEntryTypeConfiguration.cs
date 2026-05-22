using GhostfolioSidekick.Database.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	public class ExternalDataCacheEntryTypeConfiguration : IEntityTypeConfiguration<ExternalDataCacheEntry>
	{
		public void Configure(EntityTypeBuilder<ExternalDataCacheEntry> builder)
		{
			builder.ToTable("ExternalDataCacheEntries");
			builder.HasKey(e => e.Id);
			builder.Property(e => e.Key)
				.IsRequired();
			builder.Property(e => e.DataJson)
				.IsRequired()
				.HasColumnType("BLOB");
			builder.Property(e => e.CreatedAt)
				.IsRequired();
			builder.Property(e => e.ExpiresAt)
				.IsRequired();

			builder.HasIndex(e => e.Key).HasDatabaseName("IX_ExternalDataCacheEntry_Key");


		}
	}
}
