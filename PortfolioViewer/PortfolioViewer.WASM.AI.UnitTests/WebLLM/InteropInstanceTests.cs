using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.UnitTests.WebLLM
{
	/// <summary>
	/// Unit tests for InteropInstance class
	/// </summary>
	public class InteropInstanceTests
	{
		private readonly Mock<IProgress<InitializeProgress>> _mockProgress;
		private readonly InteropInstance _interopInstance;
		private const double Tolerance = 0.001;

		public InteropInstanceTests()
		{
			_mockProgress = new Mock<IProgress<InitializeProgress>>();
			_interopInstance = new InteropInstance(_mockProgress.Object);
		}

		[Fact]
		public void Constructor_ShouldInitializeWebLLMCompletionsQueue()
		{
			// Assert
			Assert.NotNull(_interopInstance.WebLLMCompletions);
			Assert.Empty(_interopInstance.WebLLMCompletions);
		}

		[Fact]
		public void ReportProgress_WithValidProgress_ShouldCallProgress()
		{
			// Arrange
			var progressReport = new InitProgressReport(0.5, "Loading model...", 1000);

			// Act
			_interopInstance.ReportProgress(progressReport);

			// Assert
			_mockProgress.Verify(p => p.Report(It.Is<InitializeProgress>(ip =>
				Math.Abs(ip.Progress - 0.5) < Tolerance && ip.Message == "Loading model...")), Times.Once);
		}

		[Fact]
		public void ReportProgress_WithNullProgress_ShouldThrowArgumentNullException()
		{
			// Act & Assert
			Assert.Throws<ArgumentNullException>(() => _interopInstance.ReportProgress(null!));
		}

		[Fact]
		public void ReportProgress_WithMaxProgress_ShouldCapAt99Percent()
		{
			// Arrange
			var progressReport = new InitProgressReport(1.5, "Overloaded progress", 1000);

			// Act
			_interopInstance.ReportProgress(progressReport);

			// Assert
			_mockProgress.Verify(p => p.Report(It.Is<InitializeProgress>(ip =>
				Math.Abs(ip.Progress - 0.99) < Tolerance)), Times.Once);
		}

		[Fact]
		public void ReportProgress_WithFinishLoadingText_ShouldReport100Percent()
		{
			// Arrange
			var progressReport = new InitProgressReport(0.8, "Finish loading on WebGPU device", 1000);

			// Act
			_interopInstance.ReportProgress(progressReport);

			// Assert
			_mockProgress.Verify(p => p.Report(It.Is<InitializeProgress>(ip =>
				Math.Abs(ip.Progress - 1.0) < Tolerance)), Times.Once);
		}

		[Fact]
		public void ReceiveChunkCompletion_WithValidCompletion_ShouldEnqueueToCompletions()
		{
			// Arrange
			var completion = new WebLLMCompletion("id", "object", "model", "fingerprint", null, null);

			// Act
			_interopInstance.ReceiveChunkCompletion(completion);

			// Assert
			Assert.Single(_interopInstance.WebLLMCompletions);
			Assert.True(_interopInstance.WebLLMCompletions.TryDequeue(out var dequeuedCompletion));
			Assert.Same(completion, dequeuedCompletion);
		}

		[Fact]
		public void ReceiveChunkCompletion_WithNullCompletion_ShouldThrowArgumentNullException()
		{
			// Act & Assert
			Assert.Throws<ArgumentNullException>(() => _interopInstance.ReceiveChunkCompletion(null!));
		}

		[Fact]
		public void ReceiveChunkCompletion_WithMultipleCompletions_ShouldEnqueueAll()
		{
			// Arrange
			var completion1 = new WebLLMCompletion("id1", "object", "model", "fingerprint", null, null);
			var completion2 = new WebLLMCompletion("id2", "object", "model", "fingerprint", null, null);

			// Act
			_interopInstance.ReceiveChunkCompletion(completion1);
			_interopInstance.ReceiveChunkCompletion(completion2);

			// Assert
			Assert.Equal(2, _interopInstance.WebLLMCompletions.Count);
		}
	}

	/// <summary>
	/// Unit tests for WebLLM record types
	/// </summary>
	public class WebLLMRecordTests
	{
		private const double Tolerance = 0.001;

		[Fact]
		public void WebLLMCompletion_WithUsage_ShouldBeStreamComplete()
		{
			// Arrange
			var usage = new Usage(10, 5, 15);
			var completion = new WebLLMCompletion("id", "object", "model", "fingerprint", null, usage);

			// Assert
			Assert.True(completion.IsStreamComplete);
		}

		[Fact]
		public void WebLLMCompletion_WithoutUsage_ShouldNotBeStreamComplete()
		{
			// Arrange
			var completion = new WebLLMCompletion("id", "object", "model", "fingerprint", null, null);

			// Assert
			Assert.False(completion.IsStreamComplete);
		}

		[Fact]
		public void InitProgressReport_ShouldCreateCorrectly()
		{
			// Act
			var report = new InitProgressReport(0.75, "Loading...", 500);

			// Assert
			Assert.True(Math.Abs(report.Progress - 0.75) < Tolerance);
			Assert.Equal("Loading...", report.Text);
			Assert.Equal(500, report.timeElapsed);
		}

		[Fact]
		public void Message_ShouldCreateCorrectly()
		{
			// Act
			var message = new Message("user", "Hello world");

			// Assert
			Assert.Equal("user", message.Role);
			Assert.Equal("Hello world", message.Content);
		}

		[Fact]
		public void Delta_ShouldCreateCorrectly()
		{
			// Act
			var delta = new Delta("assistant", "Response");

			// Assert
			Assert.Equal("assistant", delta.Role);
			Assert.Equal("Response", delta.Content);
		}

		[Fact]
		public void Usage_ShouldCreateCorrectly()
		{
			// Act
			var usage = new Usage(100, 50, 150);

			// Assert
			Assert.Equal(100, usage.CompletionTokens);
			Assert.Equal(50, usage.PromptTokens);
			Assert.Equal(150, usage.TotalTokens);
		}

		[Fact]
		public void Choice_ShouldCreateCorrectly()
		{
			// Arrange
			var message = new Message("assistant", "Hello");

			// Act
			var choice = new Choice(0, message, "logprobs", "stop");

			// Assert
			Assert.Equal(0, choice.Index);
			Assert.Equal(message, choice.Delta);
			Assert.Equal("logprobs", choice.Logprobs);
			Assert.Equal("stop", choice.FinishReason);
		}

		[Fact]
		public void WebLLMCompletion_ShouldCreateCorrectly()
		{
			// Arrange
			var choice = new Choice(0, new Message("assistant", "Hello"), "", "stop");
			var usage = new Usage(10, 5, 15);

			// Act
			var completion = new WebLLMCompletion("test-id", "completion", "test-model", "fingerprint", [choice], usage);

			// Assert
			Assert.Equal("test-id", completion.Id);
			Assert.Equal("completion", completion.Object);
			Assert.Equal("test-model", completion.Model);
			Assert.Equal("fingerprint", completion.SystemFingerprint);
			Assert.Single(completion.Choices!);
			Assert.Equal(usage, completion.Usage);
			Assert.True(completion.IsStreamComplete);
		}
	}
}