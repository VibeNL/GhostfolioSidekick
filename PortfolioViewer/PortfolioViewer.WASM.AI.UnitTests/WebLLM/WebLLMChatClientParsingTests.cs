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
		public async Task GetStreamingResponseAsync_WithToolCallJson_EmitsFunctionCallContent()
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

			var invocationCount = 0;
			var toolMock = AIFunctionFactory.Create(
				(string param) => { invocationCount++; return Task.FromResult($"Tool result for {param}"); },
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

			// The client emits the parsed call as FunctionCallContent; execution is delegated to the framework's function invocation middleware.
			var fcc = updates.SelectMany(u => u.Contents).OfType<FunctionCallContent>().Single();
			fcc.Name.Should().Be("my_tool");
			fcc.CallId.Should().Be("call_001");
			fcc.Arguments!["param"].Should().Be("value");
			invocationCount.Should().Be(0);
		}

		[Fact]
		public async Task GetResponseAsync_WithToolCallJson_PreservesFunctionCallContent()
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

			var response = await _client.GetResponseAsync(messages, options, TestContext.Current.CancellationToken);

			response.Messages.Single().Contents.OfType<FunctionCallContent>().Should().ContainSingle()
				.Which.Name.Should().Be("my_tool");
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

			var fcc = updates.SelectMany(u => u.Contents).OfType<FunctionCallContent>().Single();
			fcc.Name.Should().Be("my_tool");
			fcc.Arguments!["x"].Should().Be(42L);
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

			var fcc = updates.SelectMany(u => u.Contents).OfType<FunctionCallContent>().Single();
			fcc.Name.Should().Be("my_tool");
			fcc.Arguments!["param"].Should().Be("hello");
		}

		[Fact]
		public async Task GetStreamingResponseAsync_WithUnknownToolName_EmitsFunctionCallContent()
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

			// Unknown tools are still surfaced as FunctionCallContent; the framework middleware decides how to handle them.
			var fcc = updates.SelectMany(u => u.Contents).OfType<FunctionCallContent>().Single();
			fcc.Name.Should().Be("nonexistent_tool");
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

		// ── Tool result message conversion (framework function invocation loopback) ─

		[Fact]
		public void PrepareMessages_WithToolResultMessage_RendersAsUserMessage()
		{
			var messages = new List<ChatMessage>
			{
				new(ChatRole.User, "What's my portfolio worth?"),
				new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("call_1", "get_portfolio_summary", new Dictionary<string, object?>())]),
				new ChatMessage(ChatRole.Tool, [new FunctionResultContent("call_1", "{\"total\": 1000}")]),
			};

			var method = typeof(WebLLMChatClient).GetMethod("PrepareMessages", BindingFlags.NonPublic | BindingFlags.Static)!;
			var result = (List<ChatMessage>)method.Invoke(null, [messages, (object?)null])!;

			result.Should().NotContain(m => m.Role == ChatRole.Tool);
			result.Should().HaveCount(2);
			result[1].Role.Should().Be(ChatRole.User);
			result[1].Text!.Should().Contain("Here are the results from the data tools");
			result[1].Text.Should().Contain("{\"total\": 1000}");
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_client?.Dispose();
		}
	}
}
