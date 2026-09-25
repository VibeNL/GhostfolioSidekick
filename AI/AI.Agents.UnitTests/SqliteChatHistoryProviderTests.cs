using System.Text.Json;
using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.AI;
using Microsoft.Agents.AI;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Moq;

namespace GhostfolioSidekick.AI.Agents.UnitTests
{
	public class SqliteChatHistoryProviderTests : IDisposable
	{
		private const string ConversationId = "test-conversation";

		private readonly SqliteConnection _connection;
		private readonly DbContextOptions<DatabaseContext> _options;
		private readonly IDbContextFactory<DatabaseContext> _factory;
		private readonly SqliteChatHistoryProvider _provider;

		public SqliteChatHistoryProviderTests()
		{
			_connection = new SqliteConnection("DataSource=:memory:");
			_connection.Open();
			_options = new DbContextOptionsBuilder<DatabaseContext>().UseSqlite(_connection).Options;

			using (var db = new DatabaseContext(_options))
			{
				db.Database.Migrate();
			}

			_factory = new SharedConnectionDbContextFactory(_options);
			_provider = new SqliteChatHistoryProvider(_factory, ConversationId);
		}

		private sealed class SharedConnectionDbContextFactory : IDbContextFactory<DatabaseContext>
		{
			private readonly DbContextOptions<DatabaseContext> _options;

			public SharedConnectionDbContextFactory(DbContextOptions<DatabaseContext> options)
			{
				_options = options;
			}

			public DatabaseContext CreateDbContext() => new(_options);
		}

		public void Dispose()
		{
			_connection.Dispose();
		}

		private static ICustomChatClient CreateMockChatClient(string responseText)
		{
			var mock = new Mock<ICustomChatClient>();
			mock.Setup(x => x.Clone()).Returns(mock.Object);
			mock.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, responseText)]));

			return mock.Object;
		}

		private static string Serialize(ChatMessage message) => JsonSerializer.Serialize(message, AIJsonUtilities.DefaultOptions);

		[Fact]
		public async Task AgentRun_PersistsRequestAndResponse()
		{
			var agent = CreateMockChatClient("hi there").AsAIAgent(new ChatClientAgentOptions { Name = "test", ChatHistoryProvider = _provider });

			await agent.RunAsync("hello", cancellationToken: TestContext.Current.CancellationToken);

			var messages = await _provider.LoadMessagesAsync(TestContext.Current.CancellationToken);
			Assert.Equal(2, messages.Count);
			Assert.Equal(ChatRole.User, messages[0].Role);
			Assert.Equal("hello", messages[0].Text);
			Assert.Equal(ChatRole.Assistant, messages[1].Role);
			Assert.Equal("hi there", messages[1].Text);
		}

		[Fact]
		public async Task LoadMessages_ReturnsStoredMessagesInOrder()
		{
			await using var db = new DatabaseContext(_options);
			db.ChatMessages.Add(new ChatMessageRecord { ConversationId = ConversationId, OrderIndex = 0, ContentJson = Serialize(new ChatMessage(ChatRole.User, "first") { AuthorName = "User" }) });
			db.ChatMessages.Add(new ChatMessageRecord { ConversationId = ConversationId, OrderIndex = 1, ContentJson = Serialize(new ChatMessage(ChatRole.Assistant, "second")) });
			await db.SaveChangesAsync(TestContext.Current.CancellationToken);

			var messages = await _provider.LoadMessagesAsync(TestContext.Current.CancellationToken);

			Assert.Equal(2, messages.Count);
			Assert.Equal("first", messages[0].Text);
			Assert.Equal("User", messages[0].AuthorName);
			Assert.Equal(ChatRole.Assistant, messages[1].Role);
			Assert.Equal("second", messages[1].Text);
		}

		[Fact]
		public async Task LoadMessages_IgnoresOtherConversations()
		{
			await using var db = new DatabaseContext(_options);
			db.ChatMessages.Add(new ChatMessageRecord { ConversationId = "other", OrderIndex = 0, ContentJson = Serialize(new ChatMessage(ChatRole.User, "foreign")) });
			await db.SaveChangesAsync(TestContext.Current.CancellationToken);

			var messages = await _provider.LoadMessagesAsync(TestContext.Current.CancellationToken);

			Assert.Empty(messages);
		}

		[Fact]
		public async Task Clear_RemovesOnlyOwnConversation()
		{
			await using var db = new DatabaseContext(_options);
			db.ChatMessages.Add(new ChatMessageRecord { ConversationId = ConversationId, OrderIndex = 0, ContentJson = Serialize(new ChatMessage(ChatRole.User, "own")) });
			db.ChatMessages.Add(new ChatMessageRecord { ConversationId = "other", OrderIndex = 0, ContentJson = Serialize(new ChatMessage(ChatRole.User, "foreign")) });
			await db.SaveChangesAsync(TestContext.Current.CancellationToken);

			await _provider.ClearAsync(TestContext.Current.CancellationToken);

			var remaining = await db.ChatMessages.ToListAsync(TestContext.Current.CancellationToken);
			Assert.Single(remaining);
			Assert.Equal("other", remaining[0].ConversationId);
		}
	}
}
