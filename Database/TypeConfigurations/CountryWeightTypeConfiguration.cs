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
	internal class CountryWeightTypeConfiguration : IEntityTypeConfiguration<CountryWeight>
	{
		public void Configure(EntityTypeBuilder<CountryWeight> builder)
		{
			builder.ToTable("CountryWeights");
			builder.HasKey(x => x.Code);
		}
	}
}
