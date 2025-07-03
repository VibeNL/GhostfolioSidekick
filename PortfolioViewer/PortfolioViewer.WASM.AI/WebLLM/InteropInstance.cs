using Microsoft.Extensions.AI;
using Microsoft.JSInterop;
using System.Collections.Concurrent;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM
{
	public class InteropInstance
	{
		private readonly IProgress<InitializeProgress> _progress;

		public ConcurrentQueue<WebLLMCompletion> WebLLMCompletions { get; init; } = new();
		public ConcurrentQueue<WebLLMError> WebLLMErrors { get; init; } = new();

		public InteropInstance(IProgress<InitializeProgress> progress)
		{
			_progress = progress;
		}

		[JSInvokable]
		public void ReportProgress(InitProgressReport progress)
		{
			ArgumentNullException.ThrowIfNull(progress);

			var progressPercent = Math.Min(progress.Progress, 0.99);
			// only report done when text: Finish loading on WebGPU
			if (progress.Text.StartsWith("Finish loading on WebGPU"))
			{
				progressPercent = 1.0;
			}

			_progress.Report(new InitializeProgress(progressPercent, progress.Text));
		}

		[JSInvokable]
		public void ReportError(WebLLMErrorReport errorReport)
		{
			ArgumentNullException.ThrowIfNull(errorReport);

			var error = new WebLLMError(
				errorReport.ErrorType,
				errorReport.ErrorMessage,
				errorReport.IsRecoverable,
				errorReport.Suggestions ?? Array.Empty<string>()
			);

			WebLLMErrors.Enqueue(error);
			
			// Also report as progress with error information
			_progress.Report(new InitializeProgress(0.0, $"Error: {errorReport.ErrorMessage}"));
		}

		[JSInvokable]
		public void ReceiveChunkCompletion(WebLLMCompletion response)
		{
			ArgumentNullException.ThrowIfNull(response);

			// Add the response to the queue
			WebLLMCompletions.Enqueue(response);
		}

		internal IEnumerable<Message> ConvertMessage(IEnumerable<ChatMessage> chatMessages)
		{
			return chatMessages.Select(chatMessage =>
			{
				if (chatMessage.Role == ChatRole.User)
				{
					return new Message("user", chatMessage.Text);
				}
				else if (chatMessage.Role == ChatRole.Assistant)
				{
					return new Message("assistant", chatMessage.Text);
				}
				else if (chatMessage.Role == ChatRole.System)
				{
					return new Message("system", chatMessage.Text);
				}
				else
				{
					throw new NotSupportedException($"Chat role {chatMessage.Role} is not supported.");
				}
			});
		}
	}

	// A progress report for the initialization process
	public record InitProgressReport(double Progress, string Text, double timeElapsed);

	// An error report from JavaScript
	public record WebLLMErrorReport(
		string ErrorType,
		string ErrorMessage,
		bool IsRecoverable,
		string[]? Suggestions
	);

	// A structured error for internal use
	public record WebLLMError(
		string ErrorType,
		string ErrorMessage,
		bool IsRecoverable,
		IReadOnlyList<string> Suggestions
	);

	// A chat message
	public record Message(string Role, string Content);

	// A partial chat message
	public record Delta(string Role, string Content);
	// Chat message "cost"
	public record Usage(double CompletionTokens, double PromptTokens, double TotalTokens);
	// A collection of partial chat messages
	public record Choice(int Index, Message? Delta, string Logprobs, string FinishReason);

	// A chat completion response
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
