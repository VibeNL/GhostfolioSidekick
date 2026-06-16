using AwesomeAssertions;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests.Services
{
	public class PrivacyModeServiceTests
	{
		[Fact]
		public void IsPrivacyMode_Initially_Off()
		{
			var service = new PrivacyModeService();
			service.IsPrivacyMode.Should().BeFalse();
		}

		[Fact]
		public void Toggle_TurnsPrivacyMode_On()
		{
			var service = new PrivacyModeService();
			service.Toggle();
			service.IsPrivacyMode.Should().BeTrue();
		}

		[Fact]
		public void Toggle_TurnsPrivacyMode_Off()
		{
			var service = new PrivacyModeService();
			service.Toggle();
			service.Toggle();
			service.IsPrivacyMode.Should().BeFalse();
		}

		[Fact]
		public void Toggle_FiresOnChange_Event()
		{
			var service = new PrivacyModeService();
			var fired = false;
			service.OnChange += () => fired = true;

			service.Toggle();

			fired.Should().BeTrue();
		}

		[Fact]
		public void Toggle_FiresOnChange_EachTime()
		{
			var service = new PrivacyModeService();
			var count = 0;
			service.OnChange += () => count++;

			for (int i = 0; i < 5; i++)
			{
				service.Toggle();
			}

			count.Should().Be(5);
		}

		[Fact]
		public void Toggle_MultipleHandlers_AllFire()
		{
			var service = new PrivacyModeService();
			bool a = false, b = false;
			service.OnChange += () => a = true;
			service.OnChange += () => b = true;

			service.Toggle();

			a.Should().BeTrue();
			b.Should().BeTrue();
		}

		[Fact]
		public void Toggle_AfterUnsubscribe_HandlerDoesNotFire()
		{
			var service = new PrivacyModeService();
			var handler = new Action(() => { });
			service.OnChange += handler;
			service.OnChange -= handler;

			var act = () => service.Toggle();
			act.Should().NotThrow();
		}
	}
}
