using AwesomeAssertions;
using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.Portfolio;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.UnitTests.Portfolio
{
	public class PortfolioAgentToolProviderTests
	{
		private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
		private readonly PortfolioAgentToolProvider _sut;

		public PortfolioAgentToolProviderTests()
		{
			_scopeFactoryMock = new Mock<IServiceScopeFactory>();
			_sut = new PortfolioAgentToolProvider(_scopeFactoryMock.Object);
		}

		[Fact]
		public void ProviderName_ReturnsPortfolioData()
		{
			_sut.ProviderName.Should().Be("PortfolioData");
		}

		[Fact]
		public void ProviderDescription_IsNotEmpty()
		{
			_sut.ProviderDescription.Should().NotBeNullOrWhiteSpace();
		}

		[Fact]
		public void ImplementsIAgentToolProvider()
		{
			_sut.Should().BeAssignableTo<IAgentToolProvider>();
		}

		[Fact]
		public void GetTools_ReturnsFourTools()
		{
			var tools = _sut.GetTools();

			tools.Should().HaveCount(4);
		}

		[Fact]
		public void GetTools_ReturnsAllExpectedToolNames()
		{
			var tools = _sut.GetTools();
			var names = tools.OfType<AIFunction>().Select(t => t.Name).ToList();

			names.Should().Contain("get_holdings");
			names.Should().Contain("get_portfolio_summary");
			names.Should().Contain("get_upcoming_dividends");
			names.Should().Contain("get_portfolio_performance");
		}

		[Fact]
		public void GetTools_AllToolsAreAIFunctions()
		{
			var tools = _sut.GetTools();

			tools.Should().AllBeAssignableTo<AIFunction>();
		}

		[Fact]
		public void GetTools_ReturnsSameInstanceOnMultipleCalls()
		{
			var first = _sut.GetTools();
			var second = _sut.GetTools();

			first.Should().BeSameAs(second);
		}
	}
}
