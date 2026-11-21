using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GhostfolioSidekick.UnitTests
{
	public class SidekickHostTests
	{
		[Fact]
		public void Build_ReturnsHostWithResolvableServices()
		{
			using IHost host = SidekickHost.Build();

			Assert.NotNull(host);
			Assert.NotNull(host.Services.GetRequiredService<IApplicationSettings>());
			Assert.NotNull(host.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>());
		}
	}
}
