using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;
using System.Text.Json;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class ActivityTypeConfiguration :
		IEntityTypeConfiguration<Activity>,
		IEntityTypeConfiguration<ActivityWithQuantityAndUnitPrice>,
		IEntityTypeConfiguration<ActivityWithAmount>,
		IEntityTypeConfiguration<BuyActivity>,
		IEntityTypeConfiguration<SellActivity>,
		IEntityTypeConfiguration<CashDepositActivity>,
		IEntityTypeConfiguration<CashWithdrawalActivity>,
		IEntityTypeConfiguration<DividendActivity>,
		IEntityTypeConfiguration<FeeActivity>,
		IEntityTypeConfiguration<CorrectionActivity>,
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
		IEntityTypeConfiguration<CalculatedPriceTrace>
	{
		public void Configure(EntityTypeBuilder<Activity> builder)
		{
			builder.ToTable("Activities");
			builder.UseTphMappingStrategy();

			// Configure custom discriminator values
			builder.HasDiscriminator<string>("Discriminator")
				.HasValue<BuyActivity>("Buy")
				.HasValue<SellActivity>("Sell")
				.HasValue<CashDepositActivity>("CashDeposit")
				.HasValue<CashWithdrawalActivity>("CashWithdrawal")
				.HasValue<DividendActivity>("Dividend")
				.HasValue<FeeActivity>("Fee")
				.HasValue<CorrectionActivity>("Correction")
				.HasValue<GiftFiatActivity>("GiftFiat")
				.HasValue<GiftAssetActivity>("GiftAsset")
				.HasValue<InterestActivity>("Interest")
				.HasValue<KnownBalanceActivity>("KnownBalance")
				.HasValue<LiabilityActivity>("Liability")
				.HasValue<RepayBondActivity>("RepayBond")
				.HasValue<SendActivity>("Send")
				.HasValue<ReceiveActivity>("Receive")
				.HasValue<StakingRewardActivity>("StakingReward")
				.HasValue<ValuableActivity>("Valuable");

			builder.HasKey(a => a.Id);

			// Indexes
			builder.HasIndex(a => a.Date);
			builder.HasIndex("AccountId");
		}

		public void Configure(EntityTypeBuilder<ActivityWithQuantityAndUnitPrice> builder)
		{
			MapMoney(builder, x => x.UnitPrice, nameof(ActivityWithQuantityAndUnitPrice.UnitPrice));
			MapMoney(builder, x => x.AdjustedUnitPrice, nameof(ActivityWithQuantityAndUnitPrice.AdjustedUnitPrice));
			MapMoney(builder, x => x.TotalTransactionAmount, nameof(ActivityWithQuantityAndUnitPrice.TotalTransactionAmount));
			MapPartialSymbolIdentifiers(builder, x => x.PartialSymbolIdentifiers);

			builder.HasMany(x => x.AdjustedUnitPriceSource)
				.WithOne()
				.HasForeignKey(x => x.ActivityId)
				.OnDelete(DeleteBehavior.Cascade);
		}

		public void Configure(EntityTypeBuilder<ActivityWithAmount> builder)
		{
			MapMoney(builder, x => x.Amount, nameof(ActivityWithAmount.Amount));
		}
		
		public void Configure(EntityTypeBuilder<BuyActivity> builder)
		{
			MapMoney(builder, x => x.UnitPrice, nameof(BuyActivity.UnitPrice));
			MapMoneyList(builder, x => x.Fees, nameof(BuyActivity.Fees));
			MapMoneyList(builder, x => x.Taxes, nameof(BuyActivity.Taxes));
		}

		public void Configure(EntityTypeBuilder<SellActivity> builder)
		{
			MapMoney(builder, x => x.UnitPrice, nameof(SellActivity.UnitPrice));
			MapMoneyList(builder, x => x.Fees, nameof(SellActivity.Fees));
			MapMoneyList(builder, x => x.Taxes, nameof(SellActivity.Taxes));
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
			MapPartialSymbolIdentifiers(builder, x => x.PartialSymbolIdentifiers);
			MapMoneyList(builder, x => x.Fees, nameof(DividendActivity.Fees));
			MapMoneyList(builder, x => x.Taxes, nameof(DividendActivity.Taxes));
		}

		public void Configure(EntityTypeBuilder<FeeActivity> builder)
		{
			MapMoney(builder, x => x.Amount, nameof(FeeActivity.Amount));
		}

		public void Configure(EntityTypeBuilder<CorrectionActivity> builder)
		{
			MapMoney(builder, x => x.Amount, nameof(CorrectionActivity.Amount));
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
			MapMoney(builder, x => x.Amount, nameof(LiabilityActivity.Amount));
			MapPartialSymbolIdentifiers(builder, x => x.PartialSymbolIdentifiers);
		}

		public void Configure(EntityTypeBuilder<RepayBondActivity> builder)
		{
			MapMoney(builder, x => x.Amount, nameof(RepayBondActivity.Amount));
			MapPartialSymbolIdentifiers(builder, x => x.PartialSymbolIdentifiers);
		}

		public void Configure(EntityTypeBuilder<SendActivity> builder)
		{
			MapMoneyList(builder, x => x.Fees, nameof(SendActivity.Fees));
		}

		public void Configure(EntityTypeBuilder<ReceiveActivity> builder)
		{
			MapMoneyList(builder, x => x.Fees, nameof(ReceiveActivity.Fees));
		}

		public void Configure(EntityTypeBuilder<StakingRewardActivity> builder)
		{
		}

		public void Configure(EntityTypeBuilder<ValuableActivity> builder)
		{
			MapMoney(builder, x => x.Amount, nameof(ValuableActivity.Amount));
			MapPartialSymbolIdentifiers(builder, x => x.PartialSymbolIdentifiers);
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

		private static void MapPartialSymbolIdentifiers<TEntity>(EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, IEnumerable<PartialSymbolIdentifier>?>>? navigationExpression) where TEntity : class
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

		private static void MapMoneyList<TEntity>(EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, List<Money>>> propertyExpression, string columnName) where TEntity : class
		{
			var converter = new ValueConverter<List<Money>, string>(
				v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
				v => JsonSerializer.Deserialize<List<Money>>(v, (JsonSerializerOptions?)null) ?? new List<Money>());
			builder.Property(propertyExpression)
				.HasColumnName(columnName)
				.HasConversion(converter);
		}
	}

	public class PartialSymbolIdentifierActivity
	{
		public long Id { get; set; }
	}
}