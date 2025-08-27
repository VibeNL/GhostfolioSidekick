using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Activities.Types.MoneyLists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;
using System.Text.Json;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class ActivityTypeConfiguration :
		IEntityTypeConfiguration<Activity>,
		IEntityTypeConfiguration<ActivityWithQuantityAndUnitPrice>,
		IEntityTypeConfiguration<BuyActivity>,
		IEntityTypeConfiguration<SellActivity>,
		IEntityTypeConfiguration<CashDepositActivity>,
		IEntityTypeConfiguration<CashWithdrawalActivity>,
		IEntityTypeConfiguration<DividendActivity>,
		IEntityTypeConfiguration<FeeActivity>,
		IEntityTypeConfiguration<GiftFiatActivity>,
		IEntityTypeConfiguration<GiftAssetActivity>,
		IEntityTypeConfiguration<InterestActivity>,
		IEntityTypeConfiguration<KnownBalanceActivity>,
		IEntityTypeConfiguration<LiabilityActivity>,
		IEntityTypeConfiguration<RepayBondActivity>,
		IEntityTypeConfiguration<SendActivity>,
		IEntityTypeConfiguration<ReceiveActivity>,
		IEntityTypeConfiguration<StakingRewardActivity>,
		IEntityTypeConfiguration<ValuableActivity>,

		IEntityTypeConfiguration<BuyActivityFee>,
		IEntityTypeConfiguration<SellActivityFee>,
		IEntityTypeConfiguration<BuyActivityTax>,
		IEntityTypeConfiguration<SellActivityTax>,
		IEntityTypeConfiguration<DividendActivityFee>,
		IEntityTypeConfiguration<DividendActivityTax>,
		IEntityTypeConfiguration<SendActivityFee>,
		IEntityTypeConfiguration<ReceiveActivityFee>,

		IEntityTypeConfiguration<CalculatedPriceTrace>
	{
		public ActivityTypeConfiguration()
		{
			var stringEnumConverter = new System.Text.Json.Serialization.JsonStringEnumConverter();
		}

		public void Configure(EntityTypeBuilder<Activity> builder)
		{
			builder.ToTable("Activities");
			builder.UseTphMappingStrategy();

			builder.HasKey(a => a.Id);

			var type = typeof(Activity);
			var types = type.Assembly.GetTypes().Where(type.IsAssignableFrom);
		}

		public void Configure(EntityTypeBuilder<ActivityWithQuantityAndUnitPrice> builder)
		{
			MapMoney(builder, x => x.UnitPrice, nameof(ActivityWithQuantityAndUnitPrice.UnitPrice));
			MapMoney(builder, x => x.AdjustedUnitPrice, nameof(ActivityWithQuantityAndUnitPrice.AdjustedUnitPrice));
			MapPartialSymbolIdentifiers(builder, x => x.PartialSymbolIdentifiers, nameof(ActivityWithQuantityAndUnitPrice.PartialSymbolIdentifiers));

			builder.HasMany(x => x.AdjustedUnitPriceSource)
				.WithOne()
				.HasForeignKey(x => x.ActivityId)
				.OnDelete(DeleteBehavior.Cascade);
		}

		public void Configure(EntityTypeBuilder<BuyActivity> builder)
		{
			MapMoney(builder, x => x.UnitPrice, nameof(BuyActivity.UnitPrice));
			MapMoney(builder, x => x.TotalTransactionAmount, nameof(BuyActivity.TotalTransactionAmount));
			builder.HasMany(x => x.Fees)
				.WithOne()
				.HasForeignKey(x => x.ActivityId)
				.OnDelete(DeleteBehavior.Cascade);
			builder.HasMany(x => x.Taxes)
				.WithOne()
				.HasForeignKey(x => x.ActivityId)
				.OnDelete(DeleteBehavior.Cascade);
		}

		public void Configure(EntityTypeBuilder<SellActivity> builder)
		{
			MapMoney(builder, x => x.UnitPrice, nameof(SellActivity.UnitPrice));
			MapMoney(builder, x => x.TotalTransactionAmount, nameof(SellActivity.TotalTransactionAmount));
			builder.HasMany(x => x.Fees)
				.WithOne()
				.HasForeignKey(x => x.ActivityId)
				.OnDelete(DeleteBehavior.Cascade);
			builder.HasMany(x => x.Taxes)
				.WithOne()
				.HasForeignKey(x => x.ActivityId)
				.OnDelete(DeleteBehavior.Cascade);
		}

		public void Configure(EntityTypeBuilder<CashDepositActivity> builder)
		{
			MapMoney(builder, x => x.Amount, nameof(CashDepositActivity.Amount));
		}

		public void Configure(EntityTypeBuilder<CashWithdrawalActivity> builder)
		{
			MapMoney(builder, x => x.Amount, nameof(CashWithdrawalActivity.Amount));
		}

		public void Configure(EntityTypeBuilder<DividendActivity> builder)
		{
			MapMoney(builder, x => x.Amount, nameof(DividendActivity.Amount));
			MapPartialSymbolIdentifiers(builder, x => x.PartialSymbolIdentifiers, nameof(ActivityWithQuantityAndUnitPrice.PartialSymbolIdentifiers));
			builder.HasMany(x => x.Fees)
				.WithOne()
				.HasForeignKey(x => x.ActivityId)
				.OnDelete(DeleteBehavior.Cascade);
			builder.HasMany(x => x.Taxes)
				.WithOne()
				.HasForeignKey(x => x.ActivityId)
				.OnDelete(DeleteBehavior.Cascade);
		}

		public void Configure(EntityTypeBuilder<FeeActivity> builder)
		{
			MapMoney(builder, x => x.Amount, nameof(FeeActivity.Amount));
		}

		public void Configure(EntityTypeBuilder<GiftAssetActivity> builder)
		{
		}

		public void Configure(EntityTypeBuilder<GiftFiatActivity> builder)
		{
			MapMoney(builder, x => x.Amount, nameof(FeeActivity.Amount));
		}

		public void Configure(EntityTypeBuilder<InterestActivity> builder)
		{
			MapMoney(builder, x => x.Amount, nameof(InterestActivity.Amount));
		}

		public void Configure(EntityTypeBuilder<KnownBalanceActivity> builder)
		{
			MapMoney(builder, x => x.Amount, nameof(KnownBalanceActivity.Amount));
		}

		public void Configure(EntityTypeBuilder<LiabilityActivity> builder)
		{
			MapMoney(builder, x => x.Price, nameof(LiabilityActivity.Price));
			MapPartialSymbolIdentifiers(builder, x => x.PartialSymbolIdentifiers, nameof(ActivityWithQuantityAndUnitPrice.PartialSymbolIdentifiers));
		}

		public void Configure(EntityTypeBuilder<RepayBondActivity> builder)
		{
			MapMoney(builder, x => x.TotalRepayAmount, nameof(RepayBondActivity.TotalRepayAmount));
			MapPartialSymbolIdentifiers(builder, x => x.PartialSymbolIdentifiers, nameof(ActivityWithQuantityAndUnitPrice.PartialSymbolIdentifiers));
		}

		public void Configure(EntityTypeBuilder<SendActivity> builder)
		{
			builder.HasMany(x => x.Fees)
				.WithOne()
				.HasForeignKey(x => x.ActivityId)
				.OnDelete(DeleteBehavior.Cascade);
		}

		public void Configure(EntityTypeBuilder<ReceiveActivity> builder)
		{
			builder.HasMany(x => x.Fees)
				.WithOne()
				.HasForeignKey(x => x.ActivityId)
				.OnDelete(DeleteBehavior.Cascade);
		}

		public void Configure(EntityTypeBuilder<StakingRewardActivity> builder)
		{
		}

		public void Configure(EntityTypeBuilder<ValuableActivity> builder)
		{
			MapMoney(builder, x => x.Price, nameof(ValuableActivity.Price));
			MapPartialSymbolIdentifiers(builder, x => x.PartialSymbolIdentifiers, nameof(ActivityWithQuantityAndUnitPrice.PartialSymbolIdentifiers));
		}

		public void Configure(EntityTypeBuilder<BuyActivityFee> builder)
		{
			builder.ToTable(nameof(BuyActivityFee)+"s");
			builder.HasKey(a => a.Id);
			MapMoney(builder, x => x.Money, nameof(BuyActivityFee.Money));
		}

		public void Configure(EntityTypeBuilder<SellActivityFee> builder)
		{
			builder.ToTable(nameof(SellActivityFee) + "s");
			builder.HasKey(a => a.Id);
			MapMoney(builder, x => x.Money, nameof(SellActivityFee.Money));
		}

		public void Configure(EntityTypeBuilder<BuyActivityTax> builder)
		{
			builder.ToTable(nameof(BuyActivityTax) + "es");
			builder.HasKey(a => a.Id);
			MapMoney(builder, x => x.Money, nameof(BuyActivityTax.Money));
		}

		public void Configure(EntityTypeBuilder<SellActivityTax> builder)
		{
			builder.ToTable(nameof(SellActivityTax) + "es");
			builder.HasKey(a => a.Id);
			MapMoney(builder, x => x.Money, nameof(SellActivityTax.Money));
		}

		public void Configure(EntityTypeBuilder<DividendActivityFee> builder)
		{
			builder.ToTable(nameof(DividendActivityFee) + "s");
			builder.HasKey(a => a.Id);
			MapMoney(builder, x => x.Money, nameof(DividendActivityFee.Money));
		}

		public void Configure(EntityTypeBuilder<DividendActivityTax> builder)
		{
			builder.ToTable(nameof(DividendActivityTax) + "es");
			builder.HasKey(a => a.Id);
			MapMoney(builder, x => x.Money, nameof(DividendActivityTax.Money));
		}

		public void Configure(EntityTypeBuilder<SendActivityFee> builder)
		{
			builder.ToTable(nameof(SendActivityFee) + "s");
			builder.HasKey(a => a.Id);
			MapMoney(builder, x => x.Money, nameof(DividendActivityTax.Money));
		}

		public void Configure(EntityTypeBuilder<ReceiveActivityFee> builder)
		{
			builder.ToTable(nameof(ReceiveActivityFee) + "s");
			builder.HasKey(a => a.Id);
			MapMoney(builder, x => x.Money, nameof(DividendActivityTax.Money));
		}

		public void Configure(EntityTypeBuilder<CalculatedPriceTrace> builder)
		{
			builder.ToTable("CalculatedPriceTrace");
			builder.Property<long>("ID")
				.HasColumnType("integer")
				.ValueGeneratedOnAdd()
				.HasAnnotation("Key", 0);
			builder.HasKey("ID");

			MapMoney(builder, x => x.NewPrice, nameof(CalculatedPriceTrace.NewPrice));
		}

		private static void MapPartialSymbolIdentifiers<TEntity>(EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, IEnumerable<PartialSymbolIdentifier>?>>? navigationExpression, string name) where TEntity : class
		{
			builder.HasMany(navigationExpression)
		   .WithMany()
		   .UsingEntity<PartialSymbolIdentifierActivity>(
			   l => l.HasOne<PartialSymbolIdentifier>().WithMany().HasForeignKey("PartialSymbolIdentifierId").OnDelete(DeleteBehavior.Cascade),
			   r => r.HasOne<TEntity>().WithMany().HasForeignKey("ActivityId").OnDelete(DeleteBehavior.Cascade)
			);
		}

		private static void MapMoney<TEntity>(EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, Money?>> navigationExpression, string name) where TEntity : class
		{
			builder.ComplexProperty(navigationExpression).IsRequired().Property(x => x.Amount).HasColumnName(name);
			builder.ComplexProperty(navigationExpression).IsRequired().ComplexProperty(x => x.Currency).Property(x => x.Symbol).HasColumnName("Currency" + name);
		}
	}

	public class PartialSymbolIdentifierActivity
	{
		public long Id { get; set; }
	}
}