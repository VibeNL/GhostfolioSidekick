# LLamaSharp CPU Fallback for WebLLM

This implementation provides a CPU-based fallback using LLamaSharp when WebGPU is not available for WebLLM.

## Overview

The system automatically tries WebLLM first (with WebGPU acceleration) and falls back to LLamaSharp (CPU-only) if:

1. WebGPU is not supported in the browser
2. WebGPU initialization fails
3. WebLLM encounters runtime errors

## Setup Requirements

### 1. Model Files

You need to provide GGUF model files for LLamaSharp. Place them in one of these locations:

- `models/` directory in your application root
- `wwwroot/models/` directory 
- `~/.cache/llama/` directory (user profile)

### 2. Default Model Paths

The default configuration expects these model files:
- `llama-2-7b-chat.q4_0.gguf` for all chat modes

### 3. Custom Model Configuration

To use different models or paths, configure them during service registration:
// In Program.cs or your service configuration
services.AddWebChatClient();

// Configure custom model paths
services.ConfigureLLamaSharpModels(new Dictionary<ChatMode, string>
{
    { ChatMode.Chat, @"C:\Models\my-chat-model.gguf" },
    { ChatMode.ChatWithThiming, @"C:\Models\my-thinking-model.gguf" },
    { ChatMode.FunctionCalling, @"C:\Models\my-function-model.gguf" }
});
### 4. Example Service Registration
// In Program.cs
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add your chat client with fallback support
builder.Services.AddWebChatClient();

// Optional: Configure specific model paths
builder.Services.ConfigureLLamaSharpModels(new Dictionary<ChatMode, string>
{
    { ChatMode.Chat, "models/phi-3-mini-4k-instruct-q4.gguf" },
    { ChatMode.ChatWithThiming, "models/phi-3-mini-4k-instruct-q4.gguf" },
    { ChatMode.FunctionCalling, "models/phi-3-mini-4k-instruct-q4.gguf" }
});

await builder.Build().RunAsync();
## Recommended Models

For optimal performance in Blazor WebAssembly, use smaller quantized models:

### Recommended GGUF Models
- **Phi-3 Mini 4K Instruct Q4_0** (~2.5GB) - Microsoft's efficient model, great for WASM
- **TinyLlama 1.1B Chat Q4_0** (~800MB) - Very fast, basic functionality
- **Llama 2 7B Chat Q4_0** (~3.5GB) - Good balance of quality and performance

### Where to Download Models
- [Hugging Face Hub](https://huggingface.co/models?filter=gguf) - Search for "gguf" models
- [Microsoft Phi-3 Models](https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf)
- [TinyLlama Models](https://huggingface.co/TinyLlama/TinyLlama-1.1B-Chat-v1.0-GGUF)

### Download Example# Download Phi-3 Mini (recommended for WASM)
curl -L "https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf" -o "wwwroot/models/phi-3-mini-4k-instruct-q4.gguf"

# Or download TinyLlama for faster loading
curl -L "https://huggingface.co/TinyLlama/TinyLlama-1.1B-Chat-v1.0-GGUF/resolve/main/tinyllama-1.1b-chat-v1.0.q4_0.gguf" -o "wwwroot/models/tinyllama-1.1b-chat-v1.0.q4_0.gguf"
## Performance Considerations

### Memory Usage
- **Q4_0 quantization**: ~4 bits per parameter (recommended for WASM)
- **Q8_0 quantization**: Higher quality but ~2x memory usage
- **Q2_K quantization**: Smallest size but lower quality

### CPU Performance in WASM
- Single-threaded execution in WebAssembly
- Expect 10-50x slower inference compared to WebGPU
- Model size directly impacts loading time and memory usage
- Smaller models (< 2GB) recommended for better user experience

## Browser Compatibility

### WebLLM (Primary - GPU Accelerated)
- Chrome 113+ with WebGPU
- Edge 113+ with WebGPU
- Firefox with experimental WebGPU enabled

### LLamaSharp Fallback (CPU Only)
- All modern browsers supporting WebAssembly
- No WebGPU requirement
- Works in older browsers and mobile devices
- Compatible with Safari, Firefox, Chrome, Edge

## Error Handling & Initialization Flow

The fallback system provides graceful degradation:

1. **WebGPU Available**: Uses WebLLM for GPU acceleration
2. **WebGPU Unavailable**: Automatically switches to LLamaSharp CPU fallback
3. **No Models Available**: Provides clear error messages with suggestions

### Initialization Messages
- `"WebLLM initialized - GPU acceleration active"` - Primary system working
- `"LLamaSharp CPU fallback initialized"` - Fallback system working
- `"Error: No LLamaSharp model files found..."` - Model files missing

## Usage Example
@page "/chat"
@inject IWebChatClient chatClient
@inject ILogger<Chat> logger

<div class="chat-container">
    @if (!isInitialized)
    {
        <div class="initialization">
            <p>@initializationMessage</p>
            <div class="progress">
                <div class="progress-bar" style="width: @(initializationProgress * 100)%"></div>
            </div>
        </div>
    }
    else
    {
        <!-- Your chat UI here -->
    }
</div>

@code {
    private bool isInitialized = false;
    private string initializationMessage = "Initializing AI...";
    private double initializationProgress = 0.0;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await chatClient.InitializeAsync(new Progress<InitializeProgress>(progress =>
            {
                initializationProgress = progress.Progress;
                initializationMessage = progress.Message;
                InvokeAsync(StateHasChanged);
            }));
            
            isInitialized = true;
            logger.LogInformation("Chat client initialized successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize chat client");
            initializationMessage = $"Failed to initialize: {ex.Message}";
        }
        
        StateHasChanged();
    }

    private async Task SendMessage(string userMessage)
    {
        if (!isInitialized) return;

        var messages = new[]
        {
            new ChatMessage(ChatRole.User, userMessage)
        };

        await foreach (var response in chatClient.GetStreamingResponseAsync(messages))
        {
            // Handle streaming response
            // Update your UI with response.Text
            InvokeAsync(StateHasChanged);
        }
    }
}
## Architecture
???????????????????    ????????????????????
? FallbackClient  ? ???? Primary: WebLLM  ? (WebGPU - Fast)
?                 ?    ????????????????????
?                 ?    ????????????????????
?                 ? ???? Fallback: LLama  ? (CPU - Compatible)
???????????????????    ????????????????????
The `FallbackChatClient` automatically manages switching between implementations based on:
- Browser WebGPU support
- Model availability
- Runtime errors

## Troubleshooting

### Model Loading Issues
- **Error**: "No LLamaSharp model files found"
  - **Solution**: Download GGUF models to `wwwroot/models/` directory
  - **Check**: Verify file paths and permissions

### Memory Issues
- **Symptom**: Browser becomes unresponsive or crashes
  - **Solution**: Use smaller models (Q4_0 quantization)
  - **Solution**: Reduce context size in model parameters

### Performance Issues
- **Symptom**: Very slow response generation
  - **Expected**: CPU inference is 10-50x slower than GPU
  - **Solution**: Use smaller models like TinyLlama or Phi-3 Mini
  - **Solution**: Consider shorter input prompts

### Browser Compatibility
- **WebLLM not working**: Check WebGPU support at https://webgpureport.org/
- **LLamaSharp not working**: Ensure WebAssembly is enabled
- **Both failing**: Check browser console for detailed errors

## Configuration Reference

### Model Parameters (Optimized for WASM)
var parameters = new ModelParams(modelPath)
{
    ContextSize = 2048,      // Smaller context for WASM (default: 2048)
    GpuLayerCount = 0,       // Force CPU usage (required for WASM)
    UseMemorymap = false,    // Don't use memory mapping in WASM
    UseMemoryLock = false,   // Don't lock memory in WASM
    Threads = 1              // Single thread for WASM
};
### Inference Parameters
var inferenceParams = new InferenceParams
{
    MaxTokens = 1024         // Limit response length for better performance
};
## File Structure
wwwroot/
??? models/                          # Place your GGUF models here
?   ??? phi-3-mini-4k-instruct-q4.gguf
?   ??? tinyllama-1.1b-chat-v1.0.q4_0.gguf
?   ??? llama-2-7b-chat.q4_0.gguf
??? js/
    ??? dist/
        ??? webllm.interop.js        # WebLLM JavaScript interop
This implementation provides a robust fallback system that ensures your Blazor WebAssembly application can provide AI functionality regardless of the user's browser capabilities.