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
	internal class SymbolProfileTypeConfiguration : IEntityTypeConfiguration<SymbolProfile>
	{
		public void Configure(EntityTypeBuilder<SymbolProfile> builder)
		{
			builder.HasIndex(x => x.Symbol).IsUnique();
		}
	}
}
