using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Components.Chat
{
    public partial class ChatOverlay : ComponentBase
    {
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private WebLLMService llm { get; set; }

        private bool IsOpen = false;
        private string CurrentMessage = "";
        private bool IsBotTyping = false;

        private List<Message> Messages = new List<Message>
        {
            new Message("system", systemPrompt),
        };

        private InitProgress? progress;

        private string streamingText = "";

        private void ToggleChat()
        {
            IsOpen = !IsOpen;

            if (IsOpen)
            {
                InitializeLLMAsync();
            }
        }

        private async Task InitializeLLMAsync()
        {
            try
            {
                await llm.InitializeAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        protected override Task OnInitializedAsync()
        {
            llm.OnInitializingChanged += OnWebLLMInitialization;
            llm.OnChunkCompletion += OnChunkCompletion;
            return base.OnInitializedAsync();
        }

        private void OnWebLLMInitialization(InitProgress p)
        {
            progress = p;
            StateHasChanged();
        }

        private string GetBubbleStyle(bool isUser) =>
            $"max-width: 85%; padding: 10px 14px; border-radius: 18px; font-size: 14px; box-shadow: 0 1px 4px rgba(0,0,0,0.1); " +
            (isUser
                ? "background-color: #dbeafe; align-self: flex-end;"
                : "background-color: white; border: 1px solid #e5e7eb; align-self: flex-start;");

        private async Task OnChunkCompletion(WebLLMCompletion response)
        {
            if (response.IsStreamComplete)
            {
                IsBotTyping = false;
                Messages.Add(new Message("assistant", streamingText));
                StateHasChanged();
                streamingText = string.Empty;
                await JS.InvokeVoidAsync("scrollToBottom", "chat-messages");
            }
            else
            {
                streamingText += response.Choices?.ElementAtOrDefault(0)?.Delta?.Content ?? "";
            }

            StateHasChanged();
            await JS.InvokeVoidAsync("scrollToBottom", "chat-messages");
            await Task.CompletedTask;
        }

        private async Task StreamPromptRequest()
        {
            var input = CurrentMessage;
            Messages.Add(new Message("user", input));
            CurrentMessage = "";
            IsBotTyping = true;
            StateHasChanged();

            await llm.CompleteStreamAsync(Messages);
        }

        private static string systemPrompt =
                @"You are a helpful assistant for the Ghostfolio Sidekick.
              You can answer questions about the portfolio, stocks, and crypto.
              Be concise and to the point.
    ";

        private MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    }
}
