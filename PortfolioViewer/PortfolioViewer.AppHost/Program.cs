var builder = DistributedApplication.CreateBuilder(args);

// https://github.com/BenjaminCharlton/Aspire4Wasm

var apiService = builder.AddProject<Projects.PortfolioViewer_ApiService>("apiservice");
builder.AddProject<Projects.PortfolioViewer_WASM>("blazorServer")
						.AddWebAssemblyClient<Projects.PortfolioViewer_WASM>("webfrontend")
						.WithReference(apiService);

await builder.Build().RunAsync();
