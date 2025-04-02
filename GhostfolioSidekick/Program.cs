using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick
{
	internal static class Program
	{
		[ExcludeFromCodeCoverage]
		static Task Main(string[] args)
		{
			//var task1 = ProcessingService.Program.Main(args);
			var task2 = PortfolioViewer.ApiService.Program.Main(args);
			//var task3 = PortfolioViewer.WASM.Program.Main(args);

			return Task.WhenAll(/*task1,*/ task2/*, task3*/);
		}
	}
}
