using System;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;

namespace PortfolioViewer.WASM.UITests
{
    public class WasmTestHost : IDisposable
    {
        private readonly WebApplicationFactory<Startup> _factory;
        public string BaseUrl => _factory.Server.BaseAddress?.ToString() ?? "http://localhost";
        private bool _disposed;

        public WasmTestHost()
        {
            _factory = new WebApplicationFactory<Startup>()
                .WithWebHostBuilder(builder =>
                {
                    var webHostBuilder = builder as IWebHostBuilder;
                    if (webHostBuilder != null)
                    {
                        webHostBuilder.UseContentRoot("../../PortfolioViewer/PortfolioViewer.WASM/wwwroot");
                        webHostBuilder.UseEnvironment("Development");
                    }
                });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _factory.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
