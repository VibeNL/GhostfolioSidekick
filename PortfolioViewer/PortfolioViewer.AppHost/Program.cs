var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.PortfolioViewer_ApiService>("apiservice");

builder.AddProject<Projects.PortfolioViewer_Web>("webfrontend")
	.WithExternalHttpEndpoints()
	.WithReference(apiService)
	.WaitFor(apiService);

builder.Build().Run();
