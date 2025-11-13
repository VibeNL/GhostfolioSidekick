using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace PortfolioViewer.WASM.UITests
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3881:\"IDisposable\" should be implemented correctly", Justification = "<Pending>")]
	public class WasmTestHost : IDisposable
    {
        private readonly IHost _host;
        public string BaseUrl { get; }

        public WasmTestHost(string wasmProjectPath, int port = 5252)
        {
            BaseUrl = $"http://localhost:{port}";
            var wwwrootPath = Path.Combine(wasmProjectPath, "wwwroot");
            _host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel()
                        .UseUrls(BaseUrl)
                        .Configure(app =>
                        {
                            app.UseDefaultFiles();
                            app.UseStaticFiles(new StaticFileOptions
                            {
                                FileProvider = new PhysicalFileProvider(wwwrootPath)
                            });
                        });
                })
                .Build();
        }

        public async Task StartAsync() => await _host.StartAsync();
        public async Task StopAsync() => await _host.StopAsync();
        public void Dispose() => _host.Dispose();
    }
}
