using GhostfolioSidekick.Model.Performance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.Configuration
{
	public class PortfolioPerformanceSnapshotConfiguration : IEntityTypeConfiguration<PortfolioPerformanceSnapshot>
	{
		public void Configure(EntityTypeBuilder<PortfolioPerformanceSnapshot> builder)
		{
			builder.HasKey(e => e.Id);

			builder.Property(e => e.PortfolioHash)
				.IsRequired()
				.HasMaxLength(64); // SHA256 hash length

			builder.Property(e => e.CalculationType)
				.IsRequired()
				.HasMaxLength(50);

			builder.Property(e => e.BaseCurrency)
				.HasConversion<string>()
				.IsRequired();

			builder.Property(e => e.Scope)
				.HasConversion<string>()
				.IsRequired();

			builder.Property(e => e.ScopeIdentifier)
				.HasMaxLength(200);

			// Index for efficient lookups
			builder.HasIndex(e => new { e.PortfolioHash, e.StartDate, e.EndDate, e.BaseCurrency, e.CalculationType, e.Scope, e.ScopeIdentifier })
				.HasDatabaseName("IX_PortfolioPerformanceSnapshot_Lookup");

			// Index for finding latest versions
			builder.HasIndex(e => new { e.StartDate, e.EndDate, e.BaseCurrency, e.IsLatest, e.Scope, e.ScopeIdentifier })
				.HasDatabaseName("IX_PortfolioPerformanceSnapshot_Latest");

			// Index for historical queries
			builder.HasIndex(e => e.CalculatedAt)
				.HasDatabaseName("IX_PortfolioPerformanceSnapshot_CalculatedAt");

			 // Index for scope-based queries
			builder.HasIndex(e => new { e.Scope, e.ScopeIdentifier })
				.HasDatabaseName("IX_PortfolioPerformanceSnapshot_Scope");

			// Configure the PortfolioPerformance as an owned entity
			builder.OwnsOne(e => e.Performance, performance =>
			{
				performance.Property(p => p.TimeWeightedReturn)
					.HasPrecision(18, 8);

				performance.Property(p => p.DividendYield)
					.HasPrecision(18, 8);

				performance.Property(p => p.CurrencyImpact)
					.HasPrecision(18, 8);

				performance.OwnsOne(p => p.TotalDividends, dividend =>
				{
					dividend.Property(d => d.Amount)
						.HasPrecision(18, 8);
					dividend.Property(d => d.Currency)
						.HasConversion<string>();
				});

				performance.OwnsOne(p => p.InitialValue, initialValue =>
				{
					initialValue.Property(iv => iv.Amount)
						.HasPrecision(18, 8);
					initialValue.Property(iv => iv.Currency)
						.HasConversion<string>();
				});

				performance.OwnsOne(p => p.FinalValue, finalValue =>
				{
					finalValue.Property(fv => fv.Amount)
						.HasPrecision(18, 8);
					finalValue.Property(fv => fv.Currency)
						.HasConversion<string>();
				});

				performance.OwnsOne(p => p.NetCashFlows, netCashFlows =>
				{
					netCashFlows.Property(ncf => ncf.Amount)
						.HasPrecision(18, 8);
					netCashFlows.Property(ncf => ncf.Currency)
						.HasConversion<string>();
				});

				performance.Property(p => p.BaseCurrency)
					.HasConversion<string>();
			});
		}
	}
}