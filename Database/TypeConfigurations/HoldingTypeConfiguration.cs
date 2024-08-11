using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
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
	internal class HoldingTypeConfiguration : IEntityTypeConfiguration<Holding>
	{
		public void Configure(EntityTypeBuilder<Holding> builder)
		{
			builder.ToTable("Holdings");
			builder.HasMany(h => h.Activities).WithOne().IsRequired().OnDelete(DeleteBehavior.Cascade);
		}
	}
}
