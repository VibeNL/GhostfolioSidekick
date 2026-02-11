using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;
using System.Text.Json;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class CalculatedSnapshotTypeConfiguration : IEntityTypeConfiguration<CalculatedSnapshot>
	{
		public void Configure(EntityTypeBuilder<CalculatedSnapshot> builder)
		{
			builder.ToTable("CalculatedSnapshots");

			// Add a shadow property for the primary key since class doesn't have one
			builder.Property<long>("Id")
				.HasColumnType("integer")
				.ValueGeneratedOnAdd()
				.HasAnnotation("Key", 0);
			builder.HasKey("Id");

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