using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
    public class WakeLockService : IWakeLockService, IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private IJSObjectReference? _wakeLockModule;
        private bool _disposed = false;

        public WakeLockService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        private async Task<IJSObjectReference> GetWakeLockModuleAsync()
        {
            if (_wakeLockModule == null)
            {
                _wakeLockModule = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/wakelock.js");
            }
            return _wakeLockModule;
        }

        public async Task<bool> RequestWakeLockAsync()
        {
            try
            {
                var module = await GetWakeLockModuleAsync();
                return await module.InvokeAsync<bool>("requestWakeLock");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to request wake lock: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ReleaseWakeLockAsync()
        {
            try
            {
                var module = await GetWakeLockModuleAsync();
                return await module.InvokeAsync<bool>("releaseWakeLock");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to release wake lock: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsWakeLockSupportedAsync()
        {
            try
            {
                var module = await GetWakeLockModuleAsync();
                return await module.InvokeAsync<bool>("isWakeLockSupported");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to check wake lock support: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsWakeLockActiveAsync()
        {
            try
            {
                var module = await GetWakeLockModuleAsync();
                return await module.InvokeAsync<bool>("isWakeLockActive");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to check wake lock status: {ex.Message}");
                return false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                if (_wakeLockModule != null)
                {
                    try
                    {
                        await _wakeLockModule.InvokeVoidAsync("releaseWakeLock");
                        await _wakeLockModule.DisposeAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error disposing wake lock module: {ex.Message}");
                    }
                }
                _disposed = true;
            }
        }
    }
}