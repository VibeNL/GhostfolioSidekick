using AutoFixture;
using AutoFixture.Kernel;
using FluentAssertions;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Moq;

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

			Fixture fixture = new Fixture();
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
