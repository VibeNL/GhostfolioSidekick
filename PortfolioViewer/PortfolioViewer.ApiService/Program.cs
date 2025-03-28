using GhostfolioSidekick.Model.Accounts;
using Microsoft.AspNetCore.Authorization;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

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

app.MapGet("/weatherforecast", [AllowAnonymous] () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new Platform
		{
			Name = DateTime.Now.AddDays(index).ToString("yyyy-MM-dd"),
			Url = $"https://www.example.com/{index}",
		}
        )
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

app.Run();
