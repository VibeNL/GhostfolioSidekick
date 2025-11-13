using System;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PortfolioViewer.WASM.UITests
{
    public class WasmTestHost : IDisposable
    {
        private readonly WebApplicationFactory<GhostfolioSidekick.PortfolioViewer.WASM.Program> _factory;
        public string BaseUrl => _factory.Server.BaseAddress?.ToString() ?? "http://localhost";

        public WasmTestHost()
        {
            _factory = new WebApplicationFactory<GhostfolioSidekick.PortfolioViewer.WASM.Program>();
        }

        public void Dispose()
        {
            _factory.Dispose();
        }
    }
}
