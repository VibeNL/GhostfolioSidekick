using AutoFixture;
using AutoFixture.Kernel;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Model.UnitTests
{
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
			fixture.Customizations.Add(new ExcludeTypeSpecimenBuilder(typeof(Activity)));
			fixture.Customizations.Add(new ExcludeTypeSpecimenBuilder(typeof(Holding)));

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
