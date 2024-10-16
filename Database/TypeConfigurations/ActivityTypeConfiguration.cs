using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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
		IEntityTypeConfiguration<GiftActivity>,
		IEntityTypeConfiguration<InterestActivity>,
		IEntityTypeConfiguration<KnownBalanceActivity>,
		IEntityTypeConfiguration<LiabilityActivity>,
		IEntityTypeConfiguration<RepayBondActivity>,
		IEntityTypeConfiguration<SendAndReceiveActivity>,
		IEntityTypeConfiguration<StakingRewardActivity>,
		IEntityTypeConfiguration<ValuableActivity>

	{
		private const string UnitPrice = "UnitPrice";
		private const string Fees = "Fees";
		private const string Taxes = "Taxes";
		private const string Amount = "Amount";
		private const string Price = "Price";
		private const string PartialSymbolIdentifiers = "PartialSymbolIdentifiers";
		private readonly ValueComparer<ICollection<Money>> moneyListComparer;

		public ActivityTypeConfiguration()
		{
			moneyListComparer = new ValueComparer<ICollection<Money>>(
				(c1, c2) => c1.SequenceEqual(c2),
				c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
				c => c.ToList());
		}

		public void Configure(EntityTypeBuilder<Activity> builder)
		{
			builder.ToTable("Activities");
			var discriminatorBuilder = builder.HasDiscriminator<string>("Type");

			builder.HasKey(a => a.Id);

			var type = typeof(Activity);
			var types = type.Assembly.GetTypes().Where(type.IsAssignableFrom);

			foreach (var t in types)
			{
				discriminatorBuilder.HasValue(t, t.Name);
			}

			discriminatorBuilder.IsComplete();
		}

		public void Configure(EntityTypeBuilder<ActivityWithQuantityAndUnitPrice> builder)
		{
			builder.Property(b => b.UnitPrice)
					.HasColumnName(UnitPrice)
					.HasConversion(
						v => MoneyToString(v),
						v => StringToMoney(v));
		}

		public void Configure(EntityTypeBuilder<BuySellActivity> builder)
		{
			builder.Property(b => b.UnitPrice)
					.HasColumnName(UnitPrice)
					.HasConversion(
						v => MoneyToString(v),
						v => StringToMoney(v));

			builder.Property(b => b.Fees)
					.HasColumnName(Fees)
					.HasConversion(
						v => MoniesToString(v),
						v => StringToMonies(v),
						moneyListComparer);

			builder.Property(b => b.Taxes)
					.HasColumnName(Taxes)
					.HasConversion(
						v => MoniesToString(v),
						v => StringToMonies(v),
						moneyListComparer);
		}

		public void Configure(EntityTypeBuilder<CashDepositWithdrawalActivity> builder)
		{
			builder.Property(b => b.Amount)
					.HasColumnName(Amount)
					.HasConversion(
						v => MoneyToString(v),
						v => StringToMoney(v));
		}

		public void Configure(EntityTypeBuilder<DividendActivity> builder)
		{
			builder.Property(b => b.Amount)
					.HasColumnName(Amount)
					.HasConversion(
						v => MoneyToString(v),
						v => StringToMoney(v));

			builder.Property(b => b.Fees)
					.HasColumnName(Fees)
					.HasConversion(
						v => MoniesToString(v),
						v => StringToMonies(v),
						moneyListComparer);

			builder.Property(b => b.Taxes)
					.HasColumnName(Taxes)
					.HasConversion(
						v => MoniesToString(v),
						v => StringToMonies(v),
						moneyListComparer);
		}

		public void Configure(EntityTypeBuilder<FeeActivity> builder)
		{
			builder.Property(b => b.Amount)
					.HasColumnName(Amount)
					.HasConversion(
						v => MoneyToString(v),
						v => StringToMoney(v));
		}

		public void Configure(EntityTypeBuilder<GiftActivity> builder)
		{
			builder.Property(b => b.UnitPrice)
					.HasColumnName(UnitPrice)
					.HasConversion(
						v => MoneyToString(v),
						v => StringToMoney(v));
		}

		public void Configure(EntityTypeBuilder<InterestActivity> builder)
		{
			builder.Property(b => b.Amount)
					.HasColumnName(Amount)
					.HasConversion(
						v => MoneyToString(v),
						v => StringToMoney(v));
		}

		public void Configure(EntityTypeBuilder<KnownBalanceActivity> builder)
		{
			builder.Property(b => b.Amount)
					.HasColumnName(Amount)
					.HasConversion(
						v => MoneyToString(v),
						v => StringToMoney(v));
		}

		public void Configure(EntityTypeBuilder<LiabilityActivity> builder)
		{
			builder.Property(b => b.Price)
					.HasColumnName(Price)
					.HasConversion(
						v => MoneyToString(v),
						v => StringToMoney(v));
		}

		public void Configure(EntityTypeBuilder<RepayBondActivity> builder)
		{
			builder.Property(b => b.TotalRepayAmount)
					.HasColumnName("TotalRepayAmount")
					.HasConversion(
						v => MoneyToString(v),
						v => StringToMoney(v));
		}

		public void Configure(EntityTypeBuilder<SendAndReceiveActivity> builder)
		{
			builder.Property(b => b.UnitPrice)
					.HasColumnName(UnitPrice)
					.HasConversion(
						v => MoneyToString(v),
						v => StringToMoney(v));
			builder.Property(b => b.Fees)
					.HasColumnName(Fees)
					.HasConversion(
						v => MoniesToString(v),
						v => StringToMonies(v),
						moneyListComparer);
		}

		public void Configure(EntityTypeBuilder<StakingRewardActivity> builder)
		{
			builder.Property(b => b.UnitPrice)
					.HasColumnName(UnitPrice)
					.HasConversion(
						v => MoneyToString(v),
						v => StringToMoney(v));
		}

		public void Configure(EntityTypeBuilder<ValuableActivity> builder)
		{
			builder.Property(b => b.Price)
					.HasColumnName(Price)
					.HasConversion(
						v => MoneyToString(v),
						v => StringToMoney(v));
		}

		private ICollection<Money> StringToMonies(string v)
		{
			return JsonSerializer.Deserialize<ICollection<Money>>(v) ?? [];
		}

		private string MoniesToString(ICollection<Money> v)
		{
			return JsonSerializer.Serialize(v);
		}

		private Money StringToMoney(string v)
		{
			return JsonSerializer.Deserialize<Money>(v) ?? null!;
		}

		private string MoneyToString(Money v)
		{
			return JsonSerializer.Serialize(v);
		}
	}
}
