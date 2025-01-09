using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;
using System.Text.Json;
using System.Xml.Linq;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class ActivityTypeConfiguration :
		IEntityTypeConfiguration<Activity>,
		IEntityTypeConfiguration<ActivityWithQuantityAndUnitPrice>,
		IEntityTypeConfiguration<BuySellActivity>,
		IEntityTypeConfiguration<CashDepositWithdrawalActivity>,
		IEntityTypeConfiguration<DividendActivity>,
		IEntityTypeConfiguration<FeeActivity>,
		IEntityTypeConfiguration<GiftActivity>,
		IEntityTypeConfiguration<InterestActivity>,
		IEntityTypeConfiguration<KnownBalanceActivity>,
		IEntityTypeConfiguration<LiabilityActivity>,
		IEntityTypeConfiguration<RepayBondActivity>,
		IEntityTypeConfiguration<SendAndReceiveActivity>,
		IEntityTypeConfiguration<StakingRewardActivity>,
		IEntityTypeConfiguration<ValuableActivity>

	{
		private const string Currency = "Currency";
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
			MapMoney<ActivityWithQuantityAndUnitPrice>(builder, x => x.UnitPrice, nameof(ActivityWithQuantityAndUnitPrice.UnitPrice));
			MapMoney<ActivityWithQuantityAndUnitPrice>(builder, x => x.AdjustedUnitPrice, nameof(ActivityWithQuantityAndUnitPrice.AdjustedUnitPrice));
			MapPartialSymbolIdentifiers(builder, x => x.PartialSymbolIdentifiers, nameof(ActivityWithQuantityAndUnitPrice.PartialSymbolIdentifiers));

			builder.OwnsMany<CalculatedPriceTrace>(b => b.AdjustedUnitPriceSource, t =>
			{
				t.ToTable("CalculatedPriceTrace");
				t.Property<long>("ID").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Key", 0);
				t.HasKey("ID");
				t.OwnsOne<Money>(t => t.NewPrice, m =>
					{
						m.Property(p => p.Amount).HasColumnName("Amount");
						m.OwnsOne<Currency>(c => c.Currency, c =>
						{
							c.Property(p => p.Symbol).HasColumnName("Currency");
							c.Ignore(p => p.SourceCurrency);
							c.Ignore(p => p.Factor);
						});
					});
			});
		}

		public void Configure(EntityTypeBuilder<BuySellActivity> builder)
		{
			MapMoney<BuySellActivity>(builder, x => x.UnitPrice, nameof(BuySellActivity.UnitPrice));
			MapMoney<BuySellActivity>(builder, x => x.TotalTransactionAmount, nameof(BuySellActivity.TotalTransactionAmount));

			builder.OwnsMany<Money>(x => x.Fees, m =>
			{
				m.ToTable("BuySellActivityFees");
				m.Property<int>("ID").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Key", 0);
				m.HasKey("ID");
				m.Property(p => p.Amount).HasColumnName(nameof(BuySellActivity.Fees));
				m.OwnsOne<Currency>(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(Currency + nameof(BuySellActivity.Fees));
					c.Ignore(p => p.SourceCurrency);
					c.Ignore(p => p.Factor);
				});
			});
			builder.OwnsMany<Money>(x => x.Taxes, m =>
			{
				m.ToTable("BuySellActivityTaxes");
				m.Property<int>("ID").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Key", 0);
				m.HasKey("ID");
				m.Property(p => p.Amount).HasColumnName(nameof(BuySellActivity.Taxes));
				m.OwnsOne<Currency>(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(Currency + nameof(BuySellActivity.Taxes));
					c.Ignore(p => p.SourceCurrency);
					c.Ignore(p => p.Factor);
				});
			});
		}

		public void Configure(EntityTypeBuilder<CashDepositWithdrawalActivity> builder)
		{
			MapMoney<CashDepositWithdrawalActivity>(builder, x => x.Amount, nameof(CashDepositWithdrawalActivity.Amount));
		}

		public void Configure(EntityTypeBuilder<DividendActivity> builder)
		{
			MapMoney<DividendActivity>(builder, x => x.Amount, nameof(DividendActivity.Amount));
			MapPartialSymbolIdentifiers(builder, x => x.PartialSymbolIdentifiers, nameof(ActivityWithQuantityAndUnitPrice.PartialSymbolIdentifiers));

			builder.OwnsMany<Money>(x => x.Fees, m =>
			{
				m.ToTable("DividendActivityFees");
				m.Property<int>("ID").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Key", 0);
				m.HasKey("ID");
				m.Property(p => p.Amount).HasColumnName(nameof(DividendActivity.Fees));
				m.OwnsOne<Currency>(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(Currency + nameof(DividendActivity.Fees));
					c.Ignore(p => p.SourceCurrency);
					c.Ignore(p => p.Factor);
				});
			});

			builder.OwnsMany<Money>(x => x.Taxes, m =>
			{
				m.ToTable("DividendActivityTaxes");
				m.Property<int>("ID").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Key", 0);
				m.HasKey("ID");
				m.Property(p => p.Amount).HasColumnName(nameof(DividendActivity.Taxes));
				m.OwnsOne<Currency>(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(Currency + nameof(DividendActivity.Taxes));
					c.Ignore(p => p.SourceCurrency);
					c.Ignore(p => p.Factor);
				});
			});
		}

		public void Configure(EntityTypeBuilder<FeeActivity> builder)
		{
			MapMoney<FeeActivity>(builder, x => x.Amount, nameof(FeeActivity.Amount));
		}

		public void Configure(EntityTypeBuilder<GiftActivity> builder)
		{
		}

		public void Configure(EntityTypeBuilder<InterestActivity> builder)
		{
			MapMoney<InterestActivity>(builder, x => x.Amount, nameof(InterestActivity.Amount));
		}

		public void Configure(EntityTypeBuilder<KnownBalanceActivity> builder)
		{
			MapMoney<KnownBalanceActivity>(builder, x => x.Amount, nameof(KnownBalanceActivity.Amount));
		}

		public void Configure(EntityTypeBuilder<LiabilityActivity> builder)
		{
			MapMoney<LiabilityActivity>(builder, x => x.Price, nameof(LiabilityActivity.Price));
			MapPartialSymbolIdentifiers(builder, x => x.PartialSymbolIdentifiers, nameof(ActivityWithQuantityAndUnitPrice.PartialSymbolIdentifiers));
		}

		public void Configure(EntityTypeBuilder<RepayBondActivity> builder)
		{
			MapMoney<RepayBondActivity>(builder, x => x.TotalRepayAmount, nameof(RepayBondActivity.TotalRepayAmount));
			MapPartialSymbolIdentifiers(builder, x => x.PartialSymbolIdentifiers, nameof(ActivityWithQuantityAndUnitPrice.PartialSymbolIdentifiers));
		}

		public void Configure(EntityTypeBuilder<SendAndReceiveActivity> builder)
		{
			builder.OwnsMany<Money>(x => x.Fees, m =>
			{
				m.ToTable("SendAndReceiveActivityFees");
				m.Property<int>("ID").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Key", 0);
				m.HasKey("ID");
				m.Property(p => p.Amount).HasColumnName(nameof(SendAndReceiveActivity.Fees));
				m.OwnsOne<Currency>(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(Currency + nameof(SendAndReceiveActivity.Fees));
					c.Ignore(p => p.SourceCurrency);
					c.Ignore(p => p.Factor);
				});
			});
		}

		public void Configure(EntityTypeBuilder<StakingRewardActivity> builder)
		{
		}

		public void Configure(EntityTypeBuilder<ValuableActivity> builder)
		{
			MapMoney<ValuableActivity>(builder, x => x.Price, nameof(ValuableActivity.Price));
			MapPartialSymbolIdentifiers(builder, x => x.PartialSymbolIdentifiers, nameof(ActivityWithQuantityAndUnitPrice.PartialSymbolIdentifiers));
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
			builder.OwnsOne<Money>(navigationExpression, m =>
			{
				m.Property(p => p.Amount).HasColumnName(name);
				m.OwnsOne<Currency>(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName("Currency" + name);
					c.Ignore(p => p.SourceCurrency);
					c.Ignore(p => p.Factor);
				});
			});
		}
	}

	public class PartialSymbolIdentifierActivity
	{
		public long Id { get; set; }
	}
}