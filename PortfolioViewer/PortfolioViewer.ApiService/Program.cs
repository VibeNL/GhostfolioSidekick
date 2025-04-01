using GhostfolioSidekick.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PortfolioViewer.ApiService;
using Scalar.AspNetCore;

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

if (app.Environment.IsDevelopment())
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

app.Run();
