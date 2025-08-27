# AI Fallback Implementation

This implementation provides a robust fallback mechanism for AI chat functionality in the Blazor WebAssembly application.

## Architecture

The system implements a three-tier fallback strategy:

1. **Primary**: WebLLM (client-side AI using WebGPU)
2. **Fallback**: Wllama (client-side AI using WebAssembly)
3. **Proxy**: API service proxy for model downloads to avoid CORS issues

## Components

### FallbackChatClient
- Main entry point that manages the fallback logic
- Automatically detects WebLLM support and falls back to Wllama if needed
- Implements the `IWebChatClient` interface for seamless integration

### WebLLM Integration
- Uses `@mlc-ai/web-llm` for high-performance AI inference
- Requires WebGPU support in the browser
- Provides the best performance when available

### Wllama Integration
- Uses `@ngxson/wllama` as a fallback option
- Works on any browser with WebAssembly support
- Automatically downloads and loads models through the API proxy

### API Proxy
- Handles model downloads to avoid CORS issues in WebAssembly
- Validates model URLs for security
- Supports common AI model hosting platforms

## Configuration

The fallback system is configured in `ServiceCollectionExtensions.cs`:

```csharp
services.AddSingleton<IWebChatClient>((s) => new FallbackChatClient(
    s.GetRequiredService<IJSRuntime>(),
    s.GetRequiredService<ILoggerFactory>(),
    new Dictionary<ChatMode, string> {
        { ChatMode.Chat, "Qwen3-4B-q4f32_1-MLC" },
        { ChatMode.ChatWithThinking, "Qwen3-4B-q4f32_1-MLC" },
        { ChatMode.FunctionCalling, "Qwen3-4B-q4f32_1-MLC" },
    },
    "https://huggingface.co/ngxson/tinyllama_v0/resolve/main/tinyllama-1.1b-chat-v0.3.Q5_K_M.gguf"
));
```

## Usage

The fallback client works transparently with existing code:

```csharp
// Initialize the client
await webChatClient.InitializeAsync(progress);

// Use for streaming responses
await foreach (var response in webChatClient.GetStreamingResponseAsync(messages, options))
{
    // Handle response updates
}
```

## Browser Compatibility

| Browser | WebLLM Support | Wllama Support |
|---------|----------------|----------------|
| Chrome 113+ | ? (with WebGPU) | ? |
| Firefox | ? | ? |
| Safari | ? | ? |
| Edge 113+ | ? (with WebGPU) | ? |

## Performance Considerations

- **WebLLM**: Best performance, GPU-accelerated
- **Wllama**: Good performance, CPU-based
- **Model Size**: Wllama uses smaller models (1.1B parameters) vs WebLLM (4B parameters)
- **Download**: Models are cached after first download

## Security

- Model URLs are validated against a whitelist of trusted hosts
- Only HTTPS downloads are allowed
- The proxy controller prevents unauthorized file access

## Error Handling

The system includes comprehensive error handling:
- Automatic fallback when WebLLM fails to initialize
- Runtime fallback if WebLLM fails during operation
- Detailed logging for troubleshooting
- User-friendly error messages in the UI