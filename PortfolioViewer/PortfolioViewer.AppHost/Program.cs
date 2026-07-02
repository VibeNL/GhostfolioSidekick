var builder = DistributedApplication.CreateBuilder(args);

var blazorapp = builder.AddBlazorWasmProject<Projects.PortfolioViewer_WASM>("blazorapp");
var apiService = builder.AddProject<Projects.PortfolioViewer_ApiService>("apiservice");
builder.AddProject<Projects.PortfolioViewer_WASM>("blazorServer")
						.WithExternalHttpEndpoints()
						.WithBlazorClientApp(blazorapp)
						.WithReference(apiService);

await builder.Build().RunAsync();
