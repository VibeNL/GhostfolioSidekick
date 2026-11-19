var builder = DistributedApplication.CreateBuilder(args);

var blazorapp = builder.AddBlazorWasmApp("blazorapp", "../PortfolioViewer.WASM/PortfolioViewer.WASM.csproj");
var apiService = builder.AddDotnetProject("apiservice", "../PortfolioViewer.ApiService/PortfolioViewer.ApiService.csproj");
builder.AddDotnetProject("blazorServer", "../PortfolioViewer.WASM/PortfolioViewer.WASM.csproj")
						.WithExternalHttpEndpoints()
						.WithBlazorClientApp(blazorapp)
						.WithReference(apiService);

await builder.Build().RunAsync();
