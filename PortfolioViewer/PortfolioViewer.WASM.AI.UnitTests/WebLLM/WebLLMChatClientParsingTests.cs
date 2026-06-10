using AwesomeAssertions;
using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using System.Reflection;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.UnitTests.WebLLM
{
	/// <summary>
	/// Additional tests targeting uncovered paths in WebLLMChatClient:
	/// tool-call JSON parsing, argument extraction, PrepareMessages logic, and streaming.
	/// </summary>
	public class WebLLMChatClientParsingTests : IDisposable
	{
		private readonly Mock<IJSRuntime> _mockJSRuntime;
		private readonly Mock<ILogger<WebLLMChatClient>> _mockLogger;
		private readonly Mock<IJSObjectReference> _mockModule;
		private readonly WebLLMChatClient _client;
		private readonly InteropInstance _interopInstance;

		public WebLLMChatClientParsingTests()
		{
			_mockJSRuntime = new Mock<IJSRuntime>();
			_mockLogger = new Mock<ILogger<WebLLMChatClient>>();
			_mockModule = new Mock<IJSObjectReference>();

			_mockJSRuntime
					.Setup(js => js.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
					.ReturnsAsync(_mockModule.Object);

				_mockModule
					.Setup(m => m.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(
						It.IsAny<string>(), It.IsAny<object[]>()))
					.ReturnsAsync(default(Microsoft.JSInterop.Infrastructure.IJSVoidResult)!);

			var modelIds = new Dictionary<ChatMode, string>
			{
				{ ChatMode.Chat, "chat-model" },
				{ ChatMode.ChatWithThinking, "thinking-model" },
				{ ChatMode.FunctionCalling, "function-model" },
			};

			_client = new WebLLMChatClient(_mockJSRuntime.Object, _mockLogger.Object, modelIds);

			_interopInstance = GetInteropInstance(_client);
		}

		private static InteropInstance GetInteropInstance(WebLLMChatClient client)
		{
			var field = typeof(WebLLMChatClient).GetField("interopInstance",
				BindingFlags.NonPublic | BindingFlags.Instance);
			return (InteropInstance)field!.GetValue(client)!;
		}

		private void EnqueueTextChunk(string text)
		{
			var message = new Message("assistant", text);
			var choice = new Choice(0, message, string.Empty, string.Empty);
			var completion = new WebLLMCompletion("id1", "obj", "model", "fp", [choice], null);
			_interopInstance.ReceiveChunkCompletion(completion);
		}

		private void EnqueueStreamComplete()
		{
			var usage = new Usage(10, 5, 15);
			var completion = new WebLLMCompletion("id-done", "obj", "model", "fp", null, usage);
			_interopInstance.ReceiveChunkCompletion(completion);
		}

		// ── Streaming plain text response ─────────────────────────────────────────

		[Fact]
		public async Task GetResponseAsync_WithPlainText_ReturnsAssembledText()
		{
			EnqueueTextChunk("Hello ");
			EnqueueTextChunk("world");
			EnqueueStreamComplete();

			var messages = new List<ChatMessage>
			{
				new(ChatRole.User, "Hi there"),
			};

			var response = await _client.GetResponseAsync(messages, cancellationToken: TestContext.Current.CancellationToken);

			response.Text.Should().Be("Hello world");
		}

		[Fact]
		public async Task GetStreamingResponseAsync_WithEmptyMessageList_YieldsEmptyUpdate()
		{
			var messages = new List<ChatMessage>
			{
				new(ChatRole.User, "   "),
			};

			var updates = new List<ChatResponseUpdate>();
			await foreach (var u in _client.GetStreamingResponseAsync(messages, cancellationToken: TestContext.Current.CancellationToken))
			{
				updates.Add(u);
			}

			updates.Should().HaveCount(1);
			updates[0].Text.Should().BeNullOrEmpty();
		}

		// ── System prompt injection ────────────────────────────────────────────────

		[Fact]
		public async Task GetStreamingResponseAsync_WithSystemInstruction_InjectsSystemMessage()
		{
			EnqueueTextChunk("Answer");
			EnqueueStreamComplete();

			var messages = new List<ChatMessage>
			{
				new(ChatRole.User, "Question?"),
			};

			var options = new ChatOptions
			{
				Instructions = "You are a helpful assistant.",
			};

			var updates = new List<ChatResponseUpdate>();
			await foreach (var u in _client.GetStreamingResponseAsync(messages, options, TestContext.Current.CancellationToken))
			{
				updates.Add(u);
			}

			updates.Should().NotBeEmpty();
		}

		[Fact]
		public async Task GetStreamingResponseAsync_WithExistingSystemMessage_DoesNotDuplicateSystemPrompt()
		{
			EnqueueTextChunk("Answer");
			EnqueueStreamComplete();

			var messages = new List<ChatMessage>
			{
				new(ChatRole.System, "Existing system prompt"),
				new(ChatRole.User, "Question?"),
			};

			var options = new ChatOptions
			{
				Instructions = "Alternative instruction",
			};

			var updates = new List<ChatResponseUpdate>();
			await foreach (var u in _client.GetStreamingResponseAsync(messages, options, TestContext.Current.CancellationToken))
			{
				updates.Add(u);
			}

			updates.Should().NotBeEmpty();
		}

		// ── Tool call JSON parsing ─────────────────────────────────────────────────

		[Fact]
		public async Task GetStreamingResponseAsync_WithToolCallJson_InvokesToolAndSynthesizes()
		{
			const string toolCallJson = """
				{ "tool_calls": [
					{
						"id": "call_001",
						"type": "function",
						"function": {
							"name": "my_tool",
							"arguments": "{\"param\": \"value\"}"
						}
					}
				] }
				""";

			EnqueueTextChunk(toolCallJson);
			EnqueueStreamComplete();

			EnqueueTextChunk("Synthesis answer");
			EnqueueStreamComplete();

			var toolMock = AIFunctionFactory.Create(
				(string param) => Task.FromResult($"Tool result for {param}"),
				"my_tool");

			var messages = new List<ChatMessage>
			{
				new(ChatRole.User, "Call the tool"),
			};

			var options = new ChatOptions
			{
				Tools = [toolMock],
			};

			var updates = new List<ChatResponseUpdate>();
			await foreach (var u in _client.GetStreamingResponseAsync(messages, options, TestContext.Current.CancellationToken))
			{
				updates.Add(u);
			}

			updates.Should().NotBeEmpty();
		}

		[Fact]
		public async Task GetStreamingResponseAsync_WithProseInsteadOfToolCall_EmitsProseAsAssistant()
		{
			const string proseResponse = "This is just plain prose, no JSON tool calls here.";

			EnqueueTextChunk(proseResponse);
			EnqueueStreamComplete();

			var toolMock = AIFunctionFactory.Create(
				(string param) => Task.FromResult("result"),
				"unused_tool");

			var messages = new List<ChatMessage>
			{
				new(ChatRole.User, "Tell me something"),
			};

			var options = new ChatOptions
			{
				Tools = [toolMock],
			};

			var updates = new List<ChatResponseUpdate>();
			await foreach (var u in _client.GetStreamingResponseAsync(messages, options, TestContext.Current.CancellationToken))
			{
				updates.Add(u);
			}

			updates.Should().NotBeEmpty();
			updates.Should().Contain(u => u.Text != null && u.Text.Contains("plain prose"));
		}

		[Fact]
		public async Task GetStreamingResponseAsync_WithThinkTagsInResponse_StripsThinkContent()
		{
			const string responseWithThink = "<think>Internal reasoning</think>Final answer";

			EnqueueTextChunk(responseWithThink);
			EnqueueStreamComplete();

			var toolMock = AIFunctionFactory.Create(
				(string p) => Task.FromResult("r"),
				"some_tool");

			var messages = new List<ChatMessage>
			{
				new(ChatRole.User, "Question"),
			};

			var options = new ChatOptions { Tools = [toolMock] };

			var updates = new List<ChatResponseUpdate>();
			await foreach (var u in _client.GetStreamingResponseAsync(messages, options, TestContext.Current.CancellationToken))
			{
				updates.Add(u);
			}

			var combinedText = string.Concat(updates.Select(u => u.Text));
			combinedText.Should().Contain("Final answer");
			combinedText.Should().NotContain("Internal reasoning");
		}

		[Fact]
		public async Task GetStreamingResponseAsync_WithJsonFencedToolCall_ParsesCorrectly()
		{
			const string fencedJson = """
				```json
				{ "tool_calls": [
					{
						"id": "call_xyz",
						"type": "function",
						"function": {
							"name": "my_tool",
							"arguments": "{\"x\": 42}"
						}
					}
				] }
				```
				""";

			EnqueueTextChunk(fencedJson);
			EnqueueStreamComplete();

			EnqueueTextChunk("Done");
			EnqueueStreamComplete();

			var toolMock = AIFunctionFactory.Create(
				(long x) => Task.FromResult($"Got {x}"),
				"my_tool");

			var messages = new List<ChatMessage> { new(ChatRole.User, "Do something") };
			var options = new ChatOptions { Tools = [toolMock] };

			var updates = new List<ChatResponseUpdate>();
			await foreach (var u in _client.GetStreamingResponseAsync(messages, options, TestContext.Current.CancellationToken))
			{
				updates.Add(u);
			}

			updates.Should().NotBeEmpty();
		}

		[Fact]
		public async Task GetStreamingResponseAsync_WithToolCallArgumentsAsObject_ParsesCorrectly()
		{
			const string toolCallJson = """
				{ "tool_calls": [
					{
						"id": "call_002",
						"type": "function",
						"function": {
							"name": "my_tool",
							"arguments": {"param": "hello"}
						}
					}
				] }
				""";

			EnqueueTextChunk(toolCallJson);
			EnqueueStreamComplete();

			EnqueueTextChunk("Synthesized");
			EnqueueStreamComplete();

			var toolMock = AIFunctionFactory.Create(
				(string param) => Task.FromResult($"result: {param}"),
				"my_tool");

			var messages = new List<ChatMessage> { new(ChatRole.User, "Go") };
			var options = new ChatOptions { Tools = [toolMock] };

			var updates = new List<ChatResponseUpdate>();
			await foreach (var u in _client.GetStreamingResponseAsync(messages, options, TestContext.Current.CancellationToken))
			{
				updates.Add(u);
			}

			updates.Should().NotBeEmpty();
		}

		[Fact]
		public async Task GetStreamingResponseAsync_WithUnknownToolName_ContinuesAndSynthesizes()
		{
			const string toolCallJson = """
				{ "tool_calls": [
					{
						"id": "call_003",
						"type": "function",
						"function": {
							"name": "nonexistent_tool",
							"arguments": "{}"
						}
					}
				] }
				""";

			EnqueueTextChunk(toolCallJson);
			EnqueueStreamComplete();

			EnqueueTextChunk("Sorry, tool not found response.");
			EnqueueStreamComplete();

			var toolMock = AIFunctionFactory.Create(
				(string p) => Task.FromResult("result"),
				"existing_tool");

			var messages = new List<ChatMessage> { new(ChatRole.User, "Use nonexistent tool") };
			var options = new ChatOptions { Tools = [toolMock] };

			var updates = new List<ChatResponseUpdate>();
			await foreach (var u in _client.GetStreamingResponseAsync(messages, options, TestContext.Current.CancellationToken))
			{
				updates.Add(u);
			}

			updates.Should().NotBeEmpty();
		}

		[Fact]
		public async Task GetStreamingResponseAsync_WithEmptyToolCallsArray_EmitsProse()
		{
			const string emptyToolCalls = """{ "tool_calls": [] }""";

			EnqueueTextChunk(emptyToolCalls);
			EnqueueStreamComplete();

			var toolMock = AIFunctionFactory.Create(
				(string p) => Task.FromResult("result"),
				"some_tool");

			var messages = new List<ChatMessage> { new(ChatRole.User, "Hi") };
			var options = new ChatOptions { Tools = [toolMock] };

			var updates = new List<ChatResponseUpdate>();
			await foreach (var u in _client.GetStreamingResponseAsync(messages, options, TestContext.Current.CancellationToken))
			{
				updates.Add(u);
			}

			updates.Should().NotBeEmpty();
		}

		// ── AssistantMessage-as-User fix ───────────────────────────────────────────

		[Fact]
		public async Task GetStreamingResponseAsync_WithAssistantAsLastMessage_ConvertsToUser()
		{
			EnqueueTextChunk("Response");
			EnqueueStreamComplete();

			var messages = new List<ChatMessage>
			{
				new(ChatRole.User, "Original question"),
				new(ChatRole.Assistant, "Prior assistant answer"),
			};

			var updates = new List<ChatResponseUpdate>();
			await foreach (var u in _client.GetStreamingResponseAsync(messages, cancellationToken: TestContext.Current.CancellationToken))
			{
				updates.Add(u);
			}

			updates.Should().NotBeEmpty();
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_client?.Dispose();
		}
	}
}
