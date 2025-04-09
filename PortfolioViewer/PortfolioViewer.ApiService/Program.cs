using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.ServiceDefaults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
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

			builder.Services.AddCors(options =>
			{
				options.AddDefaultPolicy(builder =>
				{
					builder.AllowAnyOrigin()
						   .AllowAnyMethod()
						   .AllowAnyHeader();
				});
			});

			// Configure SQLite connection
			var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
			builder.Services.AddDbContext<DatabaseContext>(options => options.UseSqlite(connectionString));

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

			app.MapGet("/profolio", [AllowAnonymous] (DatabaseContext databaseContext) =>
			{
				return PortfolioManager.LoadPorfolio(databaseContext);
			})
			.WithName("GetPortfolio");

			app.MapDefaultEndpoints();

			app.UseStaticFiles();
			app.MapFallbackToFile("index.html");

			return app.RunAsync();
		}
	}
}