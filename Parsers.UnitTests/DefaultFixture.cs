using AutoFixture;
using AutoFixture.Kernel;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;

namespace GhostfolioSidekick.Parsers.UnitTests
{
	public static class DefaultFixture
	{
		public static Fixture Create()
		{
			var fixture = new Fixture();
			fixture.Customize<DateOnly>(composer => composer.FromFactory<DateTime>(DateOnly.FromDateTime));
			return fixture;
		}
	}

	internal static class CustomFixture
	{
		public static Fixture New()
		{
			var fixture = new Fixture();
			fixture.Behaviors
							.OfType<ThrowingRecursionBehavior>()
							.ToList()
							.ForEach(b => fixture.Behaviors.Remove(b));
			fixture.Behaviors.Add(new OmitOnRecursionBehavior());
			fixture.Customizations.Add(new ExcludeTypeSpecimenBuilder(typeof(Holding)));
			fixture.Customizations.Add(new ExcludeTypeSpecimenBuilder(typeof(BalancePrimaryCurrency)));
			fixture.Customizations.Add(new ExcludeTypeSpecimenBuilder(typeof(CalculatedSnapshotPrimaryCurrency)));

			return fixture;
		}

		private class ExcludeTypeSpecimenBuilder(Type excludedType) : ISpecimenBuilder
		{
			public object Create(object request, ISpecimenContext context)
			{
				if (request is Type type && type == excludedType)
				{
					return null!;
				}

				return new NoSpecimen();
			}
		}
	}
}
