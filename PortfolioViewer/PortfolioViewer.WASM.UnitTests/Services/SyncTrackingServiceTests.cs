using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.JSInterop;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests.Services
{
    public class SyncTrackingServiceTests
    {
        private readonly Mock<IJSRuntime> _jsRuntimeMock;
        private readonly SyncTrackingService _syncTrackingService;

        public SyncTrackingServiceTests()
        {
            _jsRuntimeMock = new Mock<IJSRuntime>();
            _syncTrackingService = new SyncTrackingService(_jsRuntimeMock.Object);
        }

        [Fact]
        public async Task GetLastSyncTimeAsync_WhenNoStoredValue_ReturnsNull()
        {
            // Arrange
            _jsRuntimeMock.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", "lastSyncTime"))
                         .ReturnsAsync((string?)null);

            // Act
            var result = await _syncTrackingService.GetLastSyncTimeAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetLastSyncTimeAsync_WhenValidStoredValue_ReturnsDateTime()
        {
            // Arrange
            var expectedDate = DateTime.Now;
            var isoString = expectedDate.ToString("O");
            _jsRuntimeMock.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", "lastSyncTime"))
                         .ReturnsAsync(isoString);

            // Act
            var result = await _syncTrackingService.GetLastSyncTimeAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDate, result.Value, TimeSpan.FromSeconds(1)); // Allow small time difference
        }

        [Fact]
        public async Task GetLastSyncTimeAsync_WhenInvalidStoredValue_ReturnsNull()
        {
            // Arrange
            _jsRuntimeMock.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", "lastSyncTime"))
                         .ReturnsAsync("invalid-date-string");

            // Act
            var result = await _syncTrackingService.GetLastSyncTimeAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetLastSyncTimeAsync_CallsLocalStorageSetItem()
        {
            // Arrange
            var syncTime = DateTime.Now;
            var expectedIsoString = syncTime.ToString("O");

            // Act
            await _syncTrackingService.SetLastSyncTimeAsync(syncTime);

            // Assert
            _jsRuntimeMock.Verify(js => js.InvokeVoidAsync("localStorage.setItem", "lastSyncTime", expectedIsoString), Times.Once);
        }

        [Fact]
        public async Task HasEverSyncedAsync_WhenNoStoredValue_ReturnsFalse()
        {
            // Arrange
            _jsRuntimeMock.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", "lastSyncTime"))
                         .ReturnsAsync((string?)null);

            // Act
            var result = await _syncTrackingService.HasEverSyncedAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HasEverSyncedAsync_WhenValidStoredValue_ReturnsTrue()
        {
            // Arrange
            var isoString = DateTime.Now.ToString("O");
            _jsRuntimeMock.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", "lastSyncTime"))
                         .ReturnsAsync(isoString);

            // Act
            var result = await _syncTrackingService.HasEverSyncedAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetLastSyncTimeAsync_WhenJSRuntimeThrows_ReturnsNull()
        {
            // Arrange
            _jsRuntimeMock.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", "lastSyncTime"))
                         .ThrowsAsync(new InvalidOperationException());

            // Act
            var result = await _syncTrackingService.GetLastSyncTimeAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetLastSyncTimeAsync_WhenJSRuntimeThrows_DoesNotThrow()
        {
            // Arrange
            var syncTime = DateTime.Now;
            _jsRuntimeMock.Setup(js => js.InvokeVoidAsync("localStorage.setItem", "lastSyncTime", It.IsAny<string>()))
                         .ThrowsAsync(new InvalidOperationException());

            // Act & Assert - Should not throw
            await _syncTrackingService.SetLastSyncTimeAsync(syncTime);
        }
    }
}