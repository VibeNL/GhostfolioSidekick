using AutoFixture;
using AutoFixture.Kernel;

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
			fixture.Customizations.Add(new ExcludeTypeSpecimenBuilder(typeof(Holding)));
			return fixture;
		}

		private class ExcludeTypeSpecimenBuilder : ISpecimenBuilder
		{
			private readonly Type _excludedType;

			public ExcludeTypeSpecimenBuilder(Type excludedType)
			{
				_excludedType = excludedType;
			}

			public object Create(object request, ISpecimenContext context)
			{
				if (request is Type type && type == _excludedType)
				{
					return null!;
				}

				return new NoSpecimen();
			}
		}
	}
}
