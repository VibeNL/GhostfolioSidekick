using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
