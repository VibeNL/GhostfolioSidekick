using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI
{
	public interface IWebChatClient: IChatClient
	{
		IWebChatClient Clone();

		ChatMode ChatMode { get; set; }

		Task InitializeAsync(IProgress<InitializeProgress> OnProgress);
	}

	public enum ChatMode
	{
		Chat,
		ChatWithThinking,
		FunctionCalling
	}
}
