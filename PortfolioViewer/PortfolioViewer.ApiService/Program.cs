using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.ApiService.Services;
using GhostfolioSidekick.PortfolioViewer.ServiceDefaults;
using GhostfolioSidekick.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Scalar.AspNetCore;

namespace GhostfolioSidekick.PortfolioViewer.ApiService
{
	public class Program
	{
		public static Task Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add service defaults & Aspire client integrations.
			builder.AddServiceDefaults();

			// Add services to the container.
			builder.Services.AddProblemDetails();

			// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
			builder.Services.AddOpenApi();

			// Add gRPC services
			builder.Services.AddGrpc();

			// Register configuration helper
			builder.Services.AddSingleton<IApplicationSettings, ApplicationSettings>();
			builder.Services.AddSingleton<IConfigurationHelper, ConfigurationHelper>();

			// Register ApplicationSettings
			builder.Services.AddSingleton<IApplicationSettings, ApplicationSettings>();

			builder.Services.AddCors(options =>
			{
				options.AddDefaultPolicy(policy =>
				{
					policy.AllowAnyOrigin()
						   .AllowAnyMethod()
						   .AllowAnyHeader()
						   .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
				});
			});

			// Configure SQLite connection using configuration helper
			builder.Services.AddDbContext<DatabaseContext>((serviceProvider, options) =>
			{
				var configHelper = serviceProvider.GetRequiredService<IConfigurationHelper>();
				var connectionString = "Data Source="+configHelper.GetConnectionString();
				options.UseSqlite(connectionString);
			});

			builder.Services.AddControllers(options =>
			{
				options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
				options.SuppressAsyncSuffixInActionNames = false;
			});

			var app = builder.Build();

			app.UseCors();

			// Configure the HTTP request pipeline.
			app.UseExceptionHandler();

			//if (app.Environment.IsDevelopment())
			{
				app.MapOpenApi();
				app.MapScalarApiReference(options =>
				{
					options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
				}
				);
			}

			// Enable gRPC-Web for browser compatibility
			app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

			// Map gRPC services
			app.MapGrpcService<SyncGrpcService>().EnableGrpcWeb();

			// Map API controllers BEFORE fallback routes
			app.MapControllers();
			app.MapDefaultEndpoints();

			// Static files and fallback should come last
			app.UseStaticFiles(new StaticFileOptions
			{
				FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
				ServeUnknownFileTypes = true, // Allow serving files with unknown MIME types
				DefaultContentType = "application/octet-stream" // Default MIME type for unknown files
			});
			app.MapFallbackToFile("index.html");

			return app.RunAsync();
		}
	}
}