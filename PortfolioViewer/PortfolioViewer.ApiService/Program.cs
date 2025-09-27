using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.PortfolioViewer.ApiService.Services;
using GhostfolioSidekick.PortfolioViewer.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
			builder.Services.AddDbContextFactory<DatabaseContext>(options =>
			{
				var configHelper = builder.Services.BuildServiceProvider().GetRequiredService<IConfigurationHelper>();
				var connectionString = "Data Source=" + configHelper.GetConnectionString();
				options.UseSqlite(connectionString);
			}, ServiceLifetime.Scoped);

			// Add currency exchange services for server-side conversion
			builder.Services.AddSingleton<MemoryCache>();
			builder.Services.AddSingleton<IMemoryCache>(x => x.GetRequiredService<MemoryCache>());
						
			// Register currency services as scoped to avoid singleton dependency issues
			builder.Services.AddScoped<ICurrencyExchange, CurrencyExchange>();

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