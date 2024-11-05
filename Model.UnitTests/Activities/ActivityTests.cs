using AutoFixture;
using AutoFixture.Kernel;
using FluentAssertions;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Model.UnitTests.Activities
{
	public class ActivityTests
	{
		[Fact]
		public void AllProperties_ShouldBeReadable()
		{
			// Arrang
			var type = typeof(Activity);
			var types = type.Assembly.GetTypes()
							.Where(p => type.IsAssignableFrom(p) && !p.IsAbstract);

			var fixture = new Fixture();
			fixture.Customizations.Add(new ExcludeTypeSpecimenBuilder(typeof(Holding)));
			foreach (var myType in types)
			{
				var activity = (Activity)fixture.Create(myType, new SpecimenContext(fixture));

				// Act & Assert
				activity.Account.Should().NotBeNull();
				activity.Date.Should().NotBe(DateTime.MinValue);
				activity.TransactionId.Should().NotBeNull();
			}
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
