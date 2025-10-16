using Microsoft.SemanticKernel.ChatCompletion;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
{
	// Simple DTO for shimbed streaming messages
	public record SimpleStreamingMessage(AuthorRole Role, string? Content, string? AuthorName);
}
