using GhostfolioSidekick.AI.Common;
using Microsoft.Agents.AI;
using Moq;

namespace GhostfolioSidekick.AI.Agents.UnitTests
{
	public class GhostfolioSidekickTests
	{
		[Fact]
		public void Create_ShouldReturnChatClientAgent_WithExpectedProperties()
		{
			// Arrange
			var mockWebChatClient = new Mock<ICustomChatClient>();
			var clonedClient = new Mock<ICustomChatClient>();
			clonedClient.SetupProperty(c => c.ChatMode);
			mockWebChatClient.Setup(c => c.Clone()).Returns(clonedClient.Object);

			var companions = new[] { ("ResearchAgent", "A researcher companion") };

			// Act
			var agent = GhostfolioSidekick.Create(mockWebChatClient.Object, companions);

			// Assert
			Assert.NotNull(agent);
			Assert.IsType<ChatClientAgent>(agent);
			Assert.Equal("GhostfolioSidekick", agent.Name);
			Assert.Contains("financial assistant", agent.Description);
		}

		[Fact]
		public void Create_ShouldIncludeCompanionsAndDateInInstructions()
		{
			// Arrange
			var mockWebChatClient = new Mock<ICustomChatClient>();
			var clonedClient = new Mock<ICustomChatClient>();
			clonedClient.SetupProperty(c => c.ChatMode);
			mockWebChatClient.Setup(c => c.Clone()).Returns(clonedClient.Object);

			var companions = new[] { ("ResearchAgent", "Performs online research") };

			// Act
			var agent = GhostfolioSidekick.Create(mockWebChatClient.Object, companions);

			// Assert
			Assert.NotNull(agent);

			// Instructions should include the UTC date in yyyy-MM-dd format
			var expectedDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
			Assert.Contains(expectedDate, agent.Instructions);

			// Instructions should list available companions and include the companion's name and description
			Assert.Contains("Available tools:", agent.Instructions);
			Assert.Contains("ResearchAgent", agent.Instructions);
			Assert.Contains("Performs online research", agent.Instructions);
		}
	}
}
