using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using GhostfolioSidekick.Database.Model;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class SymbolProfileTypeConfiguration : IEntityTypeConfiguration<SymbolProfile>
	{
		public void Configure(EntityTypeBuilder<SymbolProfile> builder)
		{
			builder.HasKey(x => x.Symbol);
		}
	}
}
