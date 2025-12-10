using GhostfolioSidekick.AI.Common;

namespace GhostfolioSidekick.AI.Agents.UnitTests
{
	public class AgentLoggerTests
	{
		[Fact]
		public void StartAgent_SetsCurrentAgentNameAndResetsFunction_TriggersEvent()
		{
			// Arrange
			var logger = new AgentLogger();
			bool eventTriggered = false;
			logger.CurrentAgentNameChanged += () => eventTriggered = true;

			// Act
			logger.StartAgent("TestAgent");

			// Assert
			Assert.Equal("TestAgent", logger.CurrentAgentName);
			Assert.Equal(string.Empty, logger.CurrentAgentFunction);
			Assert.True(eventTriggered);
		}

		[Fact]
		public void StartFunction_SetsCurrentAgentFunction_TriggersEvent()
		{
			// Arrange
			var logger = new AgentLogger();
			bool eventTriggered = false;
			logger.CurrentAgentNameChanged += () => eventTriggered = true;

			// Act
			logger.StartFunction("TestFunction");

			// Assert
			Assert.Equal("TestFunction", logger.CurrentAgentFunction);
			Assert.True(eventTriggered);
		}
	}
}
