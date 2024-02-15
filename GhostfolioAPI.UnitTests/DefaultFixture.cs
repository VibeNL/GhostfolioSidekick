using AutoFixture;
using AutoFixture.Kernel;
using GhostfolioSidekick.Model.Activities;
using System.Reflection;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests
{
	public class DefaultFixture
	{
		public static Fixture Create(ActivityType type = ActivityType.Buy)
		{
			var fixture = new Fixture();
			fixture.Customize<Contract.Activity>(composer =>
			composer
				.With(p => p.Type, Contract.ActivityType.BUY));
			fixture.Customize<Contract.SymbolProfile>(composer =>
			composer
				.With(p => p.AssetClass, AssetClass.Equity.ToString().ToUpperInvariant())
				.With(p => p.AssetSubClass, AssetSubClass.Etf.ToString().ToUpperInvariant()));
			fixture.Customizations.Add(new ActivityBuilder(type));
			return fixture;
		}
	}

	public class ActivityBuilder : ISpecimenBuilder
	{
		public ActivityBuilder(ActivityType type)
		{
			Type = type;
		}

		public ActivityType Type { get; }

		public object Create(object request, ISpecimenContext context)
		{
			var pi = request as ParameterInfo;
			if (pi == null)
			{
				return new NoSpecimen();
			}

			if (pi.Member.DeclaringType == typeof(Activity) &&
				pi.ParameterType == typeof(ActivityType))
			{
				return Type;
			}

			return new NoSpecimen();
		}
	}
}
