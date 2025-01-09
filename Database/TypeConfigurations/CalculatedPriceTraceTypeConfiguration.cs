//using GhostfolioSidekick.Model.Accounts;
//using GhostfolioSidekick.Model.Activities;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;

//namespace GhostfolioSidekick.Database.TypeConfigurations
//{
//	internal class CalculatedPriceTraceTypeConfiguration : IEntityTypeConfiguration<CalculatedPriceTrace>
//	{
//		public void Configure(EntityTypeBuilder<CalculatedPriceTrace> builder)
//		{
//			builder.ToTable("CalculatedPriceTrace");
//			builder.Property<long>("ID")
//				.HasColumnType("integer")
//				.ValueGeneratedOnAdd()
//				.HasAnnotation("Key", 0);
//			builder.HasKey("ID");


//		}
//	}
//}
