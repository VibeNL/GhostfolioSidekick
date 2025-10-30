using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.AI.Common
{
	public interface ICustomChatClient : IChatClient
	{
		ICustomChatClient Clone();

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
