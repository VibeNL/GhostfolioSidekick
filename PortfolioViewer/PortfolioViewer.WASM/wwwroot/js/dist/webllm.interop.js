import * as webllm from "https://esm.run/@mlc-ai/web-llm";
// WebLLM Module
export class WebLLMInterop {
    constructor() {
        // Callback for initialization progress
        this.initProgressCallback = (initProgress) => {
            console.log(initProgress);
            this.dotnetInstance?.invokeMethodAsync("ReportProgress", initProgress);
        };
    }
    // Initialize the engine
    async initialize(selectedModels, dotnet) {
        this.dotnetInstance = dotnet; // Store the .NET instance
        this.engine = await webllm.CreateMLCEngine(selectedModels, { initProgressCallback: this.initProgressCallback });
    }
    // Stream completion
    async completeStream(messages, modelId) {
        if (!this.engine) {
            throw new Error("Engine is not initialized.");
        }
        try {
            const chunks = await this.engine.chat.completions.create({
                messages,
                temperature: 0,
                seed: 42,
                model: modelId,
                tool_choice: "auto",
                tools: undefined,
                stream: true, // Enable streaming
                stream_options: { include_usage: true },
                extra_body: {
                    enable_thinking: true, // always include thinking in the response
                },
            });
            for await (const chunk of chunks) {
                // Assuming chunk is of type Chunk (define below if needed)
                await this.dotnetInstance?.invokeMethodAsync("ReceiveChunkCompletion", chunk);
            }
        }
        catch (error) {
            console.error("Error during streaming completion:", error);
        }
    }
}
// Singleton instance of WebLLMInterop
const webLLMInteropInstance = new WebLLMInterop();
// Export the functions
export async function initializeWebLLM(selectedModels, dotnet) {
    await webLLMInteropInstance.initialize(selectedModels, dotnet);
}
export async function completeStreamWebLLM(messages, modelId) {
    await webLLMInteropInstance.completeStream(messages, modelId);
}
//# sourceMappingURL=webllm.interop.js.map