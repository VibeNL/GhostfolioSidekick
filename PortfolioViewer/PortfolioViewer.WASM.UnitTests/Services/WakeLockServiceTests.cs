using Microsoft.JSInterop;
using AwesomeAssertions;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests.Services;

public class WakeLockServiceTests
{
	readonly Mock<IJSRuntime> _jsRuntimeMock;
	readonly Mock<IJSObjectReference> _jsModuleMock;
	readonly Mock<IJSObjectReference> _jsModuleMock2;

	public WakeLockServiceTests()
	{
		_jsRuntimeMock = new();
		_jsModuleMock = new(MockBehavior.Loose);
		_jsModuleMock2 = new(MockBehavior.Loose);
	}

	WakeLockService CreateService()
	{
		// First call = import module, subsequent calls return same module
		_jsRuntimeMock.SetupSequence(x => x.InvokeAsync<IJSObjectReference>(It.IsAny<string>(), It.IsAny<object[]>()))
			.ReturnsAsync(_jsModuleMock.Object)
			.ReturnsAsync(_jsModuleMock2.Object);

		return new WakeLockService(_jsRuntimeMock.Object);
	}

	[Fact]
	public async Task RequestWakeLockAsync_Success_ReturnsTrue()
	{
		_jsModuleMock.Setup(x => x.InvokeAsync<bool>("requestWakeLock", It.IsAny<object[]>())).ReturnsAsync(true);

		var service = CreateService();
		var result = await service.RequestWakeLockAsync();

		result.Should().BeTrue();
	}

	[Fact]
	public async Task RequestWakeLockAsync_JSException_ReturnsFalse()
	{
		_jsModuleMock.Setup(x => x.InvokeAsync<bool>("requestWakeLock", It.IsAny<object[]>())).ThrowsAsync(new JSException("Not supported"));

		var service = CreateService();
		var result = await service.RequestWakeLockAsync();

		result.Should().BeFalse();
	}

	[Fact]
	public async Task ReleaseWakeLockAsync_Success_ReturnsTrue()
	{
		_jsModuleMock.Setup(x => x.InvokeAsync<bool>("releaseWakeLock", It.IsAny<object[]>())).ReturnsAsync(true);

		var service = CreateService();
		var result = await service.ReleaseWakeLockAsync();

		result.Should().BeTrue();
	}

	[Fact]
	public async Task ReleaseWakeLockAsync_JSException_ReturnsFalse()
	{
		_jsModuleMock.Setup(x => x.InvokeAsync<bool>("releaseWakeLock", It.IsAny<object[]>())).ThrowsAsync(new JSException("Failed"));

		var service = CreateService();
		var result = await service.ReleaseWakeLockAsync();

		result.Should().BeFalse();
	}

	[Fact]
	public async Task IsWakeLockSupportedAsync_Success_ReturnsValue()
	{
		_jsModuleMock.Setup(x => x.InvokeAsync<bool>("isWakeLockSupported", It.IsAny<object[]>())).ReturnsAsync(true);

		var service = CreateService();
		var result = await service.IsWakeLockSupportedAsync();

		result.Should().BeTrue();
	}

	[Fact]
	public async Task IsWakeLockActiveAsync_Success_ReturnsValue()
	{
		_jsModuleMock.Setup(x => x.InvokeAsync<bool>("isWakeLockActive", It.IsAny<object[]>())).ReturnsAsync(true);

		var service = CreateService();
		var result = await service.IsWakeLockActiveAsync();

		result.Should().BeTrue();
	}

	[Fact]
	public async Task IsWakeLockActiveAsync_NotActive_ReturnsFalse()
	{
		_jsModuleMock.Setup(x => x.InvokeAsync<bool>("isWakeLockActive", It.IsAny<object[]>())).ReturnsAsync(false);

		var service = CreateService();
		var result = await service.IsWakeLockActiveAsync();

		result.Should().BeFalse();
	}

	[Fact]
	public async Task IsWakeLockSupportedAsync_JSException_ReturnsFalse()
	{
		_jsModuleMock.Setup(x => x.InvokeAsync<bool>("isWakeLockSupported", It.IsAny<object[]>())).ThrowsAsync(new JSException("Error"));

		var service = CreateService();
		var result = await service.IsWakeLockSupportedAsync();

		result.Should().BeFalse();
	}
}
