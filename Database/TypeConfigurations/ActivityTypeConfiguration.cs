using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhostfolioSidekick.Database.TypeConfigurations
{
	internal class ActivityTypeConfiguration :
		IEntityTypeConfiguration<Activity>,
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
		private const string AmountColumn = "Amount";
		private const string CurrencyColumn = "Currency";

		private const string FeeAmountColumn = "Amount";
		private const string TaxAmountColumn = "Amount";

		public void Configure(EntityTypeBuilder<Activity> builder)
		{
			builder.ToTable("Activities");
			var discriminatorBuilder = builder.HasDiscriminator<string>("Type");

			var type = typeof(Activity);
			var types = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(s => s.GetTypes())
				.Where(type.IsAssignableFrom);

			foreach (var t in types)
			{
				discriminatorBuilder.HasValue(t, t.Name);
			}

			discriminatorBuilder.IsComplete();

		}

		public void Configure(EntityTypeBuilder<BuySellActivity> builder)
		{
			builder.OwnsOne(b => b.UnitPrice, m =>
			{
				m.Property(p => p.Amount).HasColumnName(AmountColumn);
				m.OwnsOne(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(CurrencyColumn);
				});
			});
			builder.OwnsMany(b => b.Fees, m =>
			{
				m.ToTable("BuySellActivityFees");
				m.Property(p => p.Amount).HasColumnName(FeeAmountColumn);
				m.OwnsOne(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(CurrencyColumn);
				});
			});
			builder.OwnsMany(b => b.Taxes, m =>
			{
				m.ToTable("BuySellActivityTaxes");
				m.Property(p => p.Amount).HasColumnName(TaxAmountColumn);
				m.OwnsOne(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(CurrencyColumn);
				});
			});
		}

		public void Configure(EntityTypeBuilder<CashDepositWithdrawalActivity> builder)
		{
			builder.OwnsOne(b => b.Amount, m =>
			{
				m.Property(p => p.Amount).HasColumnName(AmountColumn);
				m.OwnsOne(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(CurrencyColumn);
				});
			});
		}

		public void Configure(EntityTypeBuilder<DividendActivity> builder)
		{
			builder.OwnsOne(b => b.Amount, m =>
			{
				m.Property(p => p.Amount).HasColumnName(AmountColumn);
				m.OwnsOne(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(CurrencyColumn);
				});
			});
			builder.OwnsMany(b => b.Fees, m =>
			{
				m.ToTable("DividendActivityFees");
				m.Property(p => p.Amount).HasColumnName(FeeAmountColumn);
				m.OwnsOne(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(CurrencyColumn);
				});
			});
			builder.OwnsMany(b => b.Taxes, m =>
			{
				m.ToTable("DividendActivityTaxes");
				m.Property(p => p.Amount).HasColumnName(TaxAmountColumn);
				m.OwnsOne(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(CurrencyColumn);
				});
			});
		}

		public void Configure(EntityTypeBuilder<FeeActivity> builder)
		{
			builder.OwnsOne(b => b.Amount, m =>
			{
				m.Property(p => p.Amount).HasColumnName(AmountColumn);
				m.OwnsOne(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(CurrencyColumn);
				});
			});
		}

		public void Configure(EntityTypeBuilder<GiftActivity> builder)
		{
			builder.OwnsOne(b => b.UnitPrice, m =>
			{
				m.Property(p => p.Amount).HasColumnName(AmountColumn);
				m.OwnsOne(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(CurrencyColumn);
				});
			});
		}

		public void Configure(EntityTypeBuilder<InterestActivity> builder)
		{
			builder.OwnsOne(b => b.Amount, m =>
			{
				m.Property(p => p.Amount).HasColumnName(AmountColumn);
				m.OwnsOne(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(CurrencyColumn);
				});
			});
		}

		public void Configure(EntityTypeBuilder<KnownBalanceActivity> builder)
		{
			builder.OwnsOne(b => b.Amount, m =>
			{
				m.Property(p => p.Amount).HasColumnName(AmountColumn);
				m.OwnsOne(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(CurrencyColumn);
				});
			});
		}

		public void Configure(EntityTypeBuilder<LiabilityActivity> builder)
		{
			builder.OwnsOne(b => b.Price, m =>
			{
				m.Property(p => p.Amount).HasColumnName(AmountColumn);
				m.OwnsOne(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(CurrencyColumn);
				});
			});
		}

		public void Configure(EntityTypeBuilder<RepayBondActivity> builder)
		{
			builder.OwnsOne(b => b.TotalRepayAmount, m =>
			{
				m.Property(p => p.Amount).HasColumnName(AmountColumn);
				m.OwnsOne(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(CurrencyColumn);
				});
			});
		}

		public void Configure(EntityTypeBuilder<SendAndReceiveActivity> builder)
		{
			builder.OwnsOne(b => b.UnitPrice, m =>
			{
				m.Property(p => p.Amount).HasColumnName(AmountColumn);
				m.OwnsOne(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(CurrencyColumn);
				});
			});
			builder.OwnsMany(b => b.Fees, m =>
			{
				m.ToTable("SendAndReceiveActivityFees");
				m.Property(p => p.Amount).HasColumnName(FeeAmountColumn);
				m.OwnsOne(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(CurrencyColumn);
				});
			});
		}

		public void Configure(EntityTypeBuilder<StakingRewardActivity> builder)
		{
			builder.OwnsOne(b => b.UnitPrice, m =>
			{
				m.Property(p => p.Amount).HasColumnName(AmountColumn);
				m.OwnsOne(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(CurrencyColumn);
				});
			});
		}

		public void Configure(EntityTypeBuilder<ValuableActivity> builder)
		{
			builder.OwnsOne(b => b.Price, m =>
			{
				m.Property(p => p.Amount).HasColumnName(AmountColumn);
				m.OwnsOne(c => c.Currency, c =>
				{
					c.Property(p => p.Symbol).HasColumnName(CurrencyColumn);
				});
			});
		}
	}
}
