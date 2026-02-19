using GhostfolioSidekick.Model.Performance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class CalculatedSnapshotTypeConfiguration : IEntityTypeConfiguration<CalculatedSnapshot>
	{
		public void Configure(EntityTypeBuilder<CalculatedSnapshot> builder)
		{
			builder.ToTable("CalculatedSnapshots");

			builder.HasKey(x => x.Id);
			builder.Property(x => x.Id) // Guid as string
				.HasColumnType("TEXT")
				.IsRequired();

			// Configure properties
			builder.Property(x => x.Date).IsRequired();
			builder.Property(x => x.Quantity).IsRequired();
			builder.Property(x => x.AverageCostPrice).IsRequired();
			builder.Property(x => x.CurrentUnitPrice).IsRequired();
			builder.Property(x => x.TotalInvested).IsRequired();
			builder.Property(x => x.TotalValue).IsRequired();
			builder.ComplexProperty(x => x.Currency).Property(x => x.Symbol).HasColumnName("Currency");

			// Indexes
			builder.HasIndex(x => new { x.Date });
			builder.HasIndex(x => new { x.AccountId, x.Date });
			builder.HasIndex(x => new { x.HoldingId, x.AccountId, x.Date }).IsUnique();
			builder.HasIndex(x => new { x.HoldingId, x.Date });
			builder.HasIndex(x => new { x.Date });
		}
	}
}