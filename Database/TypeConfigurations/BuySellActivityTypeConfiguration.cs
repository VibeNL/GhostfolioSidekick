using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class BuySellActivityTypeConfiguration : IEntityTypeConfiguration<BuySellActivity>
	{
		public void Configure(EntityTypeBuilder<BuySellActivity> builder)
		{
			builder.OwnsOne<Money>(b => b.UnitPrice, m =>
			{
				m.Property(p => p.Amount).HasColumnName("Amount");
				m.OwnsOne<Currency>(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName("Currency");
				});
			});
			builder.OwnsMany<Money>(b => b.Fees, m =>
			{
				m.Property(p => p.Amount).HasColumnName("Amount");
				m.OwnsOne<Currency>(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName("Currency");
				});
				});
			builder.OwnsMany<Money>(b => b.Taxes, m =>
			{
				m.Property(p => p.Amount).HasColumnName("Amount");
				m.OwnsOne<Currency>(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName("Currency");
				});
			});
		}
	}
}
