using GhostfolioSidekick.Model;
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
	internal class SectorWeightTypeConfiguration : IEntityTypeConfiguration<SectorWeight>
	{
		public void Configure(EntityTypeBuilder<SectorWeight> builder)
		{
			builder.ToTable("SectorWeights");
			builder.HasKey(x => x.Name);
		}
	}
}
