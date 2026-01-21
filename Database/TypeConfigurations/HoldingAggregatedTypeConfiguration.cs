using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;
using System.Text.Json;
using System.Xml.Linq;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class HoldingAggregatedTypeConfiguration :
		IEntityTypeConfiguration<HoldingAggregated>,
		IEntityTypeConfiguration<CalculatedSnapshot>
	{
		public void Configure(EntityTypeBuilder<HoldingAggregated> builder)
		{
			builder.ToTable("HoldingAggregateds");
			builder.HasKey(x => x.Id);

			builder.Property(x => x.Symbol).IsRequired();
			builder.Property(x => x.DataSource).IsRequired();

			builder.HasMany(x => x.CalculatedSnapshots)
				.WithOne()
				.HasForeignKey(x => x.HoldingAggregatedId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Property(e => e.SectorWeights)
					.HasConversion(
						v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
						v => JsonSerializer.Deserialize<List<SectorWeight>>(v, (JsonSerializerOptions)null!)!,
						new ValueComparer<ICollection<SectorWeight>>(
							(c1, c2) => c1!.SequenceEqual(c2!),
							c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
							c => (ICollection<SectorWeight>)c.ToList()));

			builder.Property(e => e.CountryWeight)
					.HasConversion(
						v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
						v => JsonSerializer.Deserialize<List<CountryWeight>>(v, (JsonSerializerOptions)null!)!,
						new ValueComparer<ICollection<CountryWeight>>(
							(c1, c2) => c1!.SequenceEqual(c2!),
							c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
							c => (ICollection<CountryWeight>)c.ToList()));

			builder.Property(x => x.AssetClass).HasConversion<string>();
			builder.Property(x => x.AssetSubClass).HasConversion<string>();
		}

		public void Configure(EntityTypeBuilder<CalculatedSnapshot> builder)
		{
			builder.ToTable("CalculatedSnapshots");

			// Add a shadow property for the primary key since class doesn't have one
			builder.Property<long>("Id")
				.HasColumnType("integer")
				.ValueGeneratedOnAdd()
				.HasAnnotation("Key", 0);
			builder.HasKey("Id");

			// Configure simple properties
			builder.Property(x => x.Date).IsRequired();
			builder.Property(x => x.Quantity).IsRequired();

			// Configure Money complex properties
			builder.ComplexProperty(x => x.Currency).Property(x => x.Symbol).HasColumnName("Currency");
			builder.Property(x => x.AverageCostPrice).IsRequired();
			builder.Property(x => x.CurrentUnitPrice).IsRequired();
			builder.Property(x => x.TotalInvested).IsRequired();
			builder.Property(x => x.TotalValue).IsRequired();

			// Indexes
			builder.HasIndex(x => new { x.Date });
			builder.HasIndex(x => new { x.AccountId, x.Date });
			builder.HasIndex(x => new { x.HoldingAggregatedId, x.AccountId, x.Date }).IsUnique();
			builder.HasIndex(x => new { x.HoldingAggregatedId, x.Date });
			builder.HasIndex(x => new { x.Date });
		}
	}
}