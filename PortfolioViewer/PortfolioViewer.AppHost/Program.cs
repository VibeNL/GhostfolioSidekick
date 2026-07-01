var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.PortfolioViewer_ApiService>("apiservice");

// Add the Blazor WASM project — Aspire 13 publishes WASM output as static files automatically
builder.AddProject<Projects.PortfolioViewer_WASM>("blazorWasm")
	.WithReference(apiService);

await builder.Build().RunAsync();
