namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public record InitProgress(float Progress, string Text, double TimeElapsed);

	// A chat message
	public record Message(string Role, string Content);
	// A partial chat message
	public record Delta(string Role, string Content);
	// Chat message "cost"
	public record Usage(double CompletionTokens, double PromptTokens, double TotalTokens);
	// A collection of partial chat messages
	public record Choice(int Index, Message? Delta, string Logprobs, string FinishReason);

	// A chat stream response
	public record WebLLMCompletion(
		string Id,
		string Object,
		string Model,
		string SystemFingerprint,
		Choice[]? Choices,
		Usage? Usage
		)
	{
		// The final part of a chat message stream will include Usage
		public bool IsStreamComplete => Usage is not null;
	}
}
