using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;
using GhostfolioSidekick.Blazor;
using GhostfolioSidekick.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Configure Entity Framework Core
builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseSqlite("Data Source=ghostfoliosidekick.db"));

builder.Services.AddScoped<DatabaseService>();

await builder.Build().RunAsync();
