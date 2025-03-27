var builder = DistributedApplication.CreateBuilder(args);

// https://github.com/BenjaminCharlton/Aspire4Wasm

var apiService = builder
		.AddProject<Projects.PortfolioViewer_ApiService>("apiservice")
		.AddWebAssemblyClient<Projects.PortfolioViewer_WASM>("webfrontend");

builder.Build().Run();
