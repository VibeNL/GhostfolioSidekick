using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.AI.Functions.OnlineSearch;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GhostfolioSidekick.AI.Agents.UnitTests
{
	public class ServiceCollectionExtentionsTests
	{
		[Fact]
		public void AddAgents_RegistersAgentLoggerAndOrchestrator()
		{
			var services = new ServiceCollection();

			services.AddSingleton<ICustomChatClient>(Mock.Of<ICustomChatClient>());

			services.AddAgents();
			var provider = services.BuildServiceProvider();

			Assert.NotNull(provider.GetService<AgentLogger>());
			Assert.NotNull(provider.GetService<AgentOrchestrator>());
		}

		[Fact]
		public void AddAgents_RegistersGoogleSearchServices()
		{
			var services = new ServiceCollection();
			services.AddAgents();
			var provider = services.BuildServiceProvider();

			// Should resolve IGoogleSearchService and GoogleSearchService
			var googleSearchService = provider.GetService<IGoogleSearchService>();
			Assert.NotNull(googleSearchService);
			Assert.IsType<GoogleSearchService>(googleSearchService);

			var concreteService = provider.GetService<GoogleSearchService>();
			Assert.NotNull(concreteService);
		}

		[Fact]
		public void AddAgents_RegistersHttpClientForGoogleSearchService()
		{
			var services = new ServiceCollection();
			services.AddAgents();
			var provider = services.BuildServiceProvider();

			// HttpClient should be available for GoogleSearchService
			var httpClient = provider.GetService<System.Net.Http.HttpClient>();
			Assert.NotNull(httpClient);
		}
	}
}
