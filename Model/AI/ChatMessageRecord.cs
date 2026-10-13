namespace GhostfolioSidekick.Model.AI
{
	public class ChatMessageRecord
	{
		public int Id { get; set; }

		public string ConversationId { get; set; } = default!;

		public int OrderIndex { get; set; }

		public string ContentJson { get; set; } = default!;
	}
}
