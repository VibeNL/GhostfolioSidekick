var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.PortfolioViewer_ApiService>("apiservice");

// Add the Blazor WASM project — Aspire 13 publishes WASM output as static files automatically.
// The WASM app uses the AppHost URL as its API base (same-origin).
builder.AddProject<Projects.PortfolioViewer_WASM>("blazorWasm");

await builder.Build().RunAsync();
