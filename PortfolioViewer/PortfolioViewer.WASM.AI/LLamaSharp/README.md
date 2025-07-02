# LLamaSharp CPU Fallback with Phi-3 Mini Auto-Download

This implementation provides a CPU-based fallback using LLamaSharp with Microsoft's Phi-3 Mini model, featuring automatic download capabilities when WebGPU is not available for WebLLM.

## ?? New Features

- **Automatic Phi-3 Mini Download**: Models are downloaded automatically from Hugging Face
- **Microsoft Phi-3 Mini**: Optimized 2.4GB model perfect for WASM
- **Smart Fallback**: WebLLM (GPU) ? LLamaSharp (CPU) ? Clear error messages
- **Progress Tracking**: Real-time download and initialization progress

## Quick Setup

### 1. Basic Configuration (Automatic Download Enabled)
// In Program.cs - This is now the default!
services.AddWebChatClient();

// The system will automatically:
// 1. Try WebLLM with WebGPU first
// 2. Fall back to LLamaSharp with CPU
// 3. Download Phi-3 Mini (2.4GB) if not found
// 4. Store in wwwroot/models/ directory
### 2. Custom Model Directory
services.AddWebChatClient();
services.UsePhi3MiniWithAutoDownload("custom/models/path");
### 3. Disable Auto-Download (Manual Models Only)
services.AddWebChatClient();
services.DisableAutoDownload();
// You must manually place phi-3-mini-4k-instruct.Q4_0.gguf in wwwroot/models/
## Model Details

### Phi-3 Mini 4K Instruct Q4_0
- **Size**: ~2.4GB (Q4_0 quantization)
- **Source**: Microsoft (Official Hugging Face)
- **Optimized for**: WebAssembly and limited resources
- **Context**: 4K tokens
- **Performance**: Excellent quality-to-size ratio

### Download URLhttps://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf
## Initialization Flow

### 1. WebLLM Attempt (Primary)? WebGPU Available    ? "WebLLM initialized - GPU acceleration active"
? WebGPU Unavailable ? Continue to fallback...
### 2. LLamaSharp Fallback (Secondary)? Model Found        ? "LLamaSharp CPU fallback initialized"
? Model Not Found    ? Download Phi-3 Mini (2.4GB)
? Download Complete  ? "LLamaSharp CPU fallback initialized"
? Download Failed    ? Clear error message with suggestions
## Usage Example
@page "/ai-chat"
@inject IWebChatClient chatClient
@inject ILogger<AiChat> logger

<div class="ai-container">
    @if (!isInitialized)
    {
        <div class="initialization-panel">
            <h3>?? Initializing AI System</h3>
            <p class="status-message">@initializationMessage</p>
            
            <div class="progress-container">
                <div class="progress-bar" style="width: @(initializationProgress * 100)%"></div>
                <span class="progress-text">@((initializationProgress * 100):F0)%</span>
            </div>
            
            @if (initializationMessage.Contains("Downloading"))
            {
                <p class="download-info">
                    <small>?? First-time setup: Downloading Phi-3 Mini model (2.4GB)</small>
                </p>
            }
        </div>
    }
    else
    {
        <div class="chat-interface">
            <div class="messages">
                @foreach (var message in messages)
                {
                    <div class="message @(message.Role == ChatRole.User ? "user" : "assistant")">
                        <strong>@message.Role:</strong> @message.Text
                    </div>
                }
            </div>
            
            <div class="input-area