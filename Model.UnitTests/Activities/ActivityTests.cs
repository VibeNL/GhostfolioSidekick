using AutoFixture.Kernel;
using AwesomeAssertions;
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

			var fixture = CustomFixture.New();
			foreach (var myType in types)
			{
				var activity = (Activity)fixture.Create(myType, new SpecimenContext(fixture));

				// Act & Assert
				activity.Account.Should().NotBeNull();
				activity.Date.Should().NotBe(DateTime.MinValue);
				activity.TransactionId.Should().NotBeNull();
			}
		}
	}
}
