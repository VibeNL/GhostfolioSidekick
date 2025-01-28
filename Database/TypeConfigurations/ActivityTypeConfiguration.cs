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
		IEntityTypeConfiguration<BuySellActivity>,
		IEntityTypeConfiguration<CashDepositWithdrawalActivity>,
		IEntityTypeConfiguration<DividendActivity>,
		IEntityTypeConfiguration<FeeActivity>,
		IEntityTypeConfiguration<GiftFiatActivity>,
		IEntityTypeConfiguration<GiftAssetActivity>,
		IEntityTypeConfiguration<InterestActivity>,
		IEntityTypeConfiguration<KnownBalanceActivity>,
		IEntityTypeConfiguration<LiabilityActivity>,
		IEntityTypeConfiguration<RepayBondActivity>,
		IEntityTypeConfiguration<SendAndReceiveActivity>,
		IEntityTypeConfiguration<StakingRewardActivity>,
		IEntityTypeConfiguration<ValuableActivity>,

		IEntityTypeConfiguration<BuySellActivityFee>,
		IEntityTypeConfiguration<BuySellActivityTax>,
		IEntityTypeConfiguration<DividendActivityFee>,
		IEntityTypeConfiguration<DividendActivityTax>,
		IEntityTypeConfiguration<SendAndReceiveActivityFee>,

		IEntityTypeConfiguration<CalculatedPriceTrace>
	{
		private readonly JsonSerializerOptions serializationOptions;

		public ActivityTypeConfiguration()
		{
			var stringEnumConverter = new System.Text.Json.Serialization.JsonStringEnumConverter();
			serializationOptions = new JsonSerializerOptions();
			serializationOptions.Converters.Add(stringEnumConverter);
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
		}

		public void Configure(EntityTypeBuilder<BuySellActivity> builder)
		{
			MapMoney(builder, x => x.UnitPrice, nameof(BuySellActivity.UnitPrice));
			MapMoney(builder, x => x.TotalTransactionAmount, nameof(BuySellActivity.TotalTransactionAmount));
		}

		public void Configure(EntityTypeBuilder<CashDepositWithdrawalActivity> builder)
		{
			MapMoney(builder, x => x.Amount, nameof(CashDepositWithdrawalActivity.Amount));
		}

		public void Configure(EntityTypeBuilder<DividendActivity> builder)
		{
			MapMoney(builder, x => x.Amount, nameof(DividendActivity.Amount));
			MapPartialSymbolIdentifiers(builder, x => x.PartialSymbolIdentifiers, nameof(ActivityWithQuantityAndUnitPrice.PartialSymbolIdentifiers));
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

		public void Configure(EntityTypeBuilder<SendAndReceiveActivity> builder)
		{
		}

		public void Configure(EntityTypeBuilder<StakingRewardActivity> builder)
		{
		}

		public void Configure(EntityTypeBuilder<ValuableActivity> builder)
		{
			MapMoney(builder, x => x.Price, nameof(ValuableActivity.Price));
			MapPartialSymbolIdentifiers(builder, x => x.PartialSymbolIdentifiers, nameof(ActivityWithQuantityAndUnitPrice.PartialSymbolIdentifiers));
		}

		public void Configure(EntityTypeBuilder<BuySellActivityFee> builder)
		{
			builder.ToTable("BuySellActivityFees");
			builder.HasKey(a => a.Id);
			MapMoney(builder, x => x.Money, nameof(BuySellActivityFee.Money));
		}

		public void Configure(EntityTypeBuilder<BuySellActivityTax> builder)
		{
			builder.ToTable("BuySellActivityTaxes");
			builder.HasKey(a => a.Id);
			MapMoney(builder, x => x.Money, nameof(BuySellActivityTax.Money));
		}

		public void Configure(EntityTypeBuilder<DividendActivityFee> builder)
		{
			builder.ToTable("DividendActivityFees");
			builder.HasKey(a => a.Id);
			MapMoney(builder, x => x.Money, nameof(DividendActivityFee.Money));
		}

		public void Configure(EntityTypeBuilder<DividendActivityTax> builder)
		{
			builder.ToTable("DividendActivityTaxex");
			builder.HasKey(a => a.Id);
			MapMoney(builder, x => x.Money, nameof(DividendActivityTax.Money));
		}

		public void Configure(EntityTypeBuilder<SendAndReceiveActivityFee> builder)
		{
			builder.ToTable("SendAndReceiveActivityFees");
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
			   l => l.HasOne<PartialSymbolIdentifier>().WithMany().HasForeignKey("PartialSymbolIdentifierId"),
			   r => r.HasOne<TEntity>().WithMany().HasForeignKey("ActivityId"));
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