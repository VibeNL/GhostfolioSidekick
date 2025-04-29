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


internal class Program
{
	private static async Task Main(string[] args)
	{
		// Create the Kernel builder
		var builder = Kernel.CreateBuilder();

		// Load the model
		string modelPath = Path.Combine("Models", "Meta-Llama-3.1-8B-Instruct-Q5_K_M.gguf");

		// Set up LLamaSharp parameters
		var parameters = new ModelParams(modelPath)
		{
			ContextSize = 8192,
			GpuLayerCount = 30 // How many layers to offload to GPU. Please adjust it according to your GPU memory.
		};
		using var model = LLamaWeights.LoadFromFile(parameters);
		using var context = model.CreateContext(parameters);
		//var executor = new InteractiveExecutor(context);
		//var executor = new StatelessExecutor(model, parameters);

		// Register the LLamaSharpTextCompletion manually
		builder.Services.AddTransient<IChatCompletionService>(_ => new LLamaSharpChatCompletion(new InteractiveExecutor(context)));

		var kernel = builder.Build();

		// 3. Definieer Agents
		string ProgamManager = """
    You are a program manager which will take the requirement and create a plan for creating app. Program Manager understands the 
    user requirements and form the detail documents with requirements and costing. 
""";

		string SoftwareEngineer = """
   You are Software Engieer, and your goal is develop web app using HTML and JavaScript (JS) by taking into consideration all
   the requirements given by Program Manager. 
""";

		string Manager = """
    You are manager which will review software engineer code, and make sure all client requirements are completed.
     Once all client requirements are completed, you can approve the request by just responding "approve"
""";

#pragma warning disable SKEXP0110, SKEXP0001 // Rethrow to preserve stack details

		ChatCompletionAgent ProgaramManagerAgent =
				   new()
				   {
					   Instructions = ProgamManager,
					   Name = "ProgramManagerAgent",
					   Kernel = kernel
				   };

		ChatCompletionAgent SoftwareEngineerAgent =
				   new()
				   {
					   Instructions = SoftwareEngineer,
					   Name = "SoftwareEngineerAgent",
					   Kernel = kernel
				   };

		ChatCompletionAgent ProjectManagerAgent =
				   new()
				   {
					   Instructions = Manager,
					   Name = "ProjectManagerAgent",
					   Kernel = kernel
				   };

		// 4. Create the AgentGroupChat
		AgentGroupChat chat =
			new(ProgaramManagerAgent, SoftwareEngineerAgent, ProjectManagerAgent)
			{
				ExecutionSettings =
					new()
					{
						TerminationStrategy =
							new ApprovalTerminationStrategy()
							{
								Agents = [ProjectManagerAgent],
								MaximumIterations = 20,
							}
					}
			};

		// === INTERACTIVE LOOP ===
		/*Console.WriteLine("👋 Welcome to the Multi-Agent Chat! Type 'exit' to quit.");

		while (true)
		{
			Console.Write("\nYou: ");
			var userInput = Console.ReadLine();

			chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userInput));
			Console.WriteLine($"# {AuthorRole.User}: '{userInput}'");

			await foreach (var content in chat.InvokeAsync())
			{
				Console.WriteLine($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'");
			}
		}*/

		string input = """
        
        I want to develop calculator app.
        Keep it very simple. And get final approval from ProjectManagerAgent on the code.
        Do not ask the user for any input.
        """;

		chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, input));
		Console.WriteLine($"# {AuthorRole.User}: '{input}'");

		await foreach (var content in chat.InvokeAsync())
		{ 
			// Set color based on the author name
			switch (content.AuthorName)
			{
				case "ProgramManagerAgent":
					Console.ForegroundColor = ConsoleColor.Cyan;
					break;
				case "SoftwareEngineerAgent":
					Console.ForegroundColor = ConsoleColor.Green;
					break;
				case "ProjectManagerAgent":
					Console.ForegroundColor = ConsoleColor.Yellow;
					break;
				default:
					Console.ForegroundColor = ConsoleColor.White; // Default color for unknown authors
					break;
			}

			Console.WriteLine($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'");

			// Reset the console color to default
			Console.ResetColor();
		}
	}

	private sealed class ApprovalTerminationStrategy : TerminationStrategy
	{
		// Terminate when the final message contains the term "approve"
		protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
			=> Task.FromResult(history[history.Count - 1].Content?.Contains("approve", StringComparison.OrdinalIgnoreCase) ?? false);
	}
}