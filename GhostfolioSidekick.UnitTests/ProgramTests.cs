using FluentAssertions;
using GhostfolioSidekick.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using RestSharp;

namespace GhostfolioSidekick.UnitTests
{
	public class ProgramTests
	{
		[Fact]
		public void CheckIfEverythingIsRegistered()
		{
			// Arrange
			var st = new Mock<IApplicationSettings>();
			st.Setup(x => x.GhostfolioUrl).Returns("https://dummy");
			st.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance());
			var rc = new Mock<IRestClient>();

			var testHost = Program
			.CreateHostBuilder()
			.ConfigureServices((hostContext, services) =>
			{
				services.AddSingleton(st.Object);
				services.AddSingleton(rc.Object);
			})
			.Build();

			// Act
			var host = testHost.Services.GetService<IHostedService>();

			//
			host.Should().NotBeNull();
		}
	}
}
