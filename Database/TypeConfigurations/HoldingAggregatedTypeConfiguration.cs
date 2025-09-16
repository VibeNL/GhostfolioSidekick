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
				.HasForeignKey("HoldingAggregatedId")
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

			// Add shadow property for foreign key
			builder.Property<long>("HoldingAggregatedId");

			// Configure simple properties
			builder.Property(x => x.Date).IsRequired();
			builder.Property(x => x.Quantity).IsRequired();

			// Configure Money complex properties
			MapMoney(builder, x => x.AverageCostPrice, nameof(CalculatedSnapshot.AverageCostPrice));
			MapMoney(builder, x => x.CurrentUnitPrice, nameof(CalculatedSnapshot.CurrentUnitPrice));
			MapMoney(builder, x => x.TotalInvested, nameof(CalculatedSnapshot.TotalInvested));
			MapMoney(builder, x => x.TotalValue, nameof(CalculatedSnapshot.TotalValue));

			// Indexes
			//builder.HasIndex(x => new { x.Date });
			//builder.HasIndex(x => new { x.AccountId, x.Date });
		}

		private static void MapMoney<TEntity>(EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, Money>> navigationExpression, string name) where TEntity : class
		{
			// Cast to nullable Money to satisfy EF Core's ComplexProperty method
			var nullableExpression = Expression.Lambda<Func<TEntity, Money?>>(
				navigationExpression.Body, 
				navigationExpression.Parameters);
			
			builder.ComplexProperty(nullableExpression).IsRequired().Property(x => x!.Amount).HasColumnName(name);
			builder.ComplexProperty(nullableExpression).IsRequired().ComplexProperty(x => x!.Currency).Property(x => x.Symbol).HasColumnName("Currency" + name);
		}
	}
}