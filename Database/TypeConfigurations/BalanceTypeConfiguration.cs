using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class BalanceTypeConfiguration : IEntityTypeConfiguration<Balance>
	{
		public void Configure(EntityTypeBuilder<Balance> builder)
		{
			builder.ToTable("Balances");
			builder.OwnsOne<Money>(b => b.Money, m =>
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
