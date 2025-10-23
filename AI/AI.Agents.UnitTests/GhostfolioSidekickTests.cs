using GhostfolioSidekick.AI.Common;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;

namespace GhostfolioSidekick.AI.Agents.UnitTests
{
	public class GhostfolioSidekickTests
	{
		[Fact]
		public void Create_ShouldReturnChatCompletionAgent_WithExpectedProperties()
		{
			// Arrange
			var mockWebChatClient = new Mock<ICustomChatClient>();
			var clonedClient = new Mock<ICustomChatClient>();
			clonedClient.SetupProperty(c => c.ChatMode);
			mockWebChatClient.Setup(c => c.Clone()).Returns(clonedClient.Object);

			var companion = new ChatCompletionAgent
			{
				Name = "ResearchAgent",
				Description = "A researcher companion"
			} as Agent;

			// Act - use fully-qualified name to avoid collision with root namespace named 'GhostfolioSidekick'
			var agent = GhostfolioSidekick.Create(mockWebChatClient.Object, new[] { companion });

			// Assert
			Assert.NotNull(agent);
			Assert.IsType<ChatCompletionAgent>(agent);
			Assert.Equal("GhostfolioSidekick", agent.Name);
			Assert.Contains("financial assistant", agent.Description);
			Assert.Equal(AuthorRole.System, agent.InstructionsRole);
			Assert.NotNull(agent.Kernel);
		}

		[Fact]
		public void Create_ShouldIncludeCompanionsAndDateInInstructions()
		{
			// Arrange
			var mockWebChatClient = new Mock<ICustomChatClient>();
			var clonedClient = new Mock<ICustomChatClient>();
			clonedClient.SetupProperty(c => c.ChatMode);
			mockWebChatClient.Setup(c => c.Clone()).Returns(clonedClient.Object);

			var companion = new ChatCompletionAgent
			{
				Name = "ResearchAgent",
				Description = "Performs online research"
			} as Agent;

			// Act - use fully-qualified name to avoid collision with root namespace named 'GhostfolioSidekick'
			var agent = GhostfolioSidekick.Create(mockWebChatClient.Object, new[] { companion });

			// Assert
			Assert.NotNull(agent);

			// Instructions should include the UTC date in yyyy-MM-dd format
			var expectedDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
			Assert.Contains(expectedDate, agent.Instructions);

			// Instructions should list available companions and include the companion's name and description
			Assert.Contains("Available companions:", agent.Instructions);
			Assert.Contains("ResearchAgent", agent.Instructions);
			Assert.Contains("Performs online research", agent.Instructions);

			// Should instruct about delegation behavior
			Assert.Contains("delegating a task to a companion", agent.Instructions);
		}
	}
}
