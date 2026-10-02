using System.Text.Json;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.AI;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.AI.Agents
{
	/// <summary>
	/// Chat history provider that persists chat messages to the SQLite database, so conversations survive page refreshes.
	/// </summary>
	public class SqliteChatHistoryProvider : ChatHistoryProvider
	{
		private const string StateKey = "SqliteChatHistoryProvider";

		private static readonly JsonSerializerOptions JsonOptions = AIJsonUtilities.DefaultOptions;

		private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
		private readonly string _conversationId;

		public SqliteChatHistoryProvider(IDbContextFactory<DatabaseContext> dbContextFactory, string conversationId)
		{
			ArgumentNullException.ThrowIfNull(dbContextFactory);
			ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);

			_dbContextFactory = dbContextFactory;
			_conversationId = conversationId;
		}

		public override IReadOnlyList<string> StateKeys => [StateKey];

		protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(InvokingContext context, CancellationToken cancellationToken = default)
		{
			return await LoadMessagesAsync(cancellationToken);
		}

		protected override async ValueTask StoreChatHistoryAsync(InvokedContext context, CancellationToken cancellationToken = default)
		{
			var newMessages = context.RequestMessages.Concat(context.ResponseMessages ?? [])
				.Where(x => !string.IsNullOrWhiteSpace(x.Text))
				.ToList();
			if (newMessages.Count == 0)
			{
				return;
			}

			await using DatabaseContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
			int nextOrderIndex = await db.ChatMessages.Where(x => x.ConversationId == _conversationId).MaxAsync(x => (int?)x.OrderIndex, cancellationToken) ?? -1;

			foreach (var message in newMessages)
			{
				db.ChatMessages.Add(new ChatMessageRecord
				{
					ConversationId = _conversationId,
					OrderIndex = ++nextOrderIndex,
					ContentJson = JsonSerializer.Serialize(message, JsonOptions)
				});
			}

			await db.SaveChangesAsync(cancellationToken);
		}

		public async Task<IReadOnlyList<ChatMessage>> LoadMessagesAsync(CancellationToken cancellationToken = default)
		{
			await using DatabaseContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
			var jsons = await db.ChatMessages
				.Where(x => x.ConversationId == _conversationId)
				.OrderBy(x => x.OrderIndex)
				.Select(x => x.ContentJson)
				.ToListAsync(cancellationToken);

			return jsons.Select(json => JsonSerializer.Deserialize<ChatMessage>(json, JsonOptions)!).ToList();
		}

		public async Task ClearAsync(CancellationToken cancellationToken = default)
		{
			await using DatabaseContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
			db.ChatMessages.RemoveRange(db.ChatMessages.Where(x => x.ConversationId == _conversationId));
			await db.SaveChangesAsync(cancellationToken);
		}
	}
}
