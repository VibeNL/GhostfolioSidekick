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

			// Act
			var agent = GhostfolioSidekick.Create(mockWebChatClient.Object);

			// Assert
			Assert.NotNull(agent);
			Assert.IsType<ChatClientAgent>(agent);
			Assert.Equal("GhostfolioSidekick", agent.Name);
			Assert.Contains("financial assistant", agent.Description);
		}
	}
}
