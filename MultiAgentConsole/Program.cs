using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Sqlite;
using LLama.Common;
using LLamaSharp.SemanticKernel.ChatCompletion;
using LLama;
using LLamaSharp.SemanticKernel.TextCompletion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.ChatCompletion;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using AuthorRole = Microsoft.SemanticKernel.ChatCompletion.AuthorRole;
using LLama.Native;
using GhostfolioSidekick.Tools.PortfolioViewer.WASM.AI.Agents;
using GhostfolioSidekick.Database;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;


internal class Program
{
	private static async Task Main(string[] args)
	{
		HostApplicationBuilder cmdBuilder = Host.CreateApplicationBuilder(args);
		cmdBuilder.Services.AddLogging(configure => configure.AddConsole());
		cmdBuilder.Services.AddDbContext<DatabaseContext>(options =>
		{
			options.UseSqlite("Data Source=database/ghostfoliosidekick.db;Pooling=true;Cache=Shared");
		});

		var app = cmdBuilder.Build();

		// Create the Kernel builder
		var builder = Kernel.CreateBuilder();
		
		// Load the model
		string modelPath = Path.Combine("Models", "Meta-Llama-3.1-8B-Instruct-Q5_K_M.gguf");

		// Set up LLamaSharp parameters
		var parameters = new ModelParams(modelPath)
		{
			ContextSize = 4096,
			GpuLayerCount = 30, // How many layers to offload to GPU. Please adjust it according to your GPU memory.
		};

		NativeLogConfig.llama_log_set((level, message) =>
		{
			
		});

		using var model = await LLamaWeights.LoadFromFileAsync(parameters);
		using var context = model.CreateContext(parameters);
		var executor = new InteractiveExecutor(context);
		//var executor = new StatelessExecutor(model, parameters);

		// Register the LLamaSharpTextCompletion manually
		builder.Services.AddTransient<IChatCompletionService>(_ => new LLamaSharpChatCompletion(new StatelessExecutor(model, parameters)));

		var kernel = builder.Build();

		var agent1 = new FinancialAgent("FinancialAgent", app.Services.GetRequiredService<ILogger<FinancialAgent>>());
		var agent2 = new GenericQueryAgent(app.Services.GetRequiredService<DatabaseContext>(), "GenericQueryAgent",
							app.Services.GetRequiredService<ILogger<GenericQueryAgent>>());
		var agent3 = new CriticAgent("CriticAgent", app.Services.GetRequiredService<ILogger<CriticAgent>>());

		var chat = await (new PortfolioAgentGroupChat(
				"PortfolioChat", 
				[agent1, agent2, agent3],
				app.Services.GetRequiredService<ILogger<PortfolioAgentGroupChat>>())
			.Initialize(kernel));

		string input = """
        What is my largest position in my portfolio?
        """;

		chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, input));
		Console.WriteLine($"# {AuthorRole.User}: '{input}'");

		await foreach (var content in chat.InvokeAsync())
		{
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
			Console.WriteLine($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'");
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
		}
	}
}