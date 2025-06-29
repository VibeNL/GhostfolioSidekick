import * as webllm from "https://esm.run/@mlc-ai/web-llm";

// Define types for the module
export interface DotNetInstance {
    invokeMethodAsync(methodName: string, ...args: any[]): Promise<void>;
}

export interface Message {
    role: string;
    content: string;
}

export interface InitProgressReport {
    progress: number;
    timeElapsed: number;
    text: string;
}

// WebLLM Module
export class WebLLMInterop {
    private engine: webllm.MLCEngine | undefined;
    private dotnetInstance: DotNetInstance | undefined;

    constructor() { }

    // Callback for initialization progress
    private initProgressCallback = (initProgress: InitProgressReport): void => {
        console.log(initProgress);
        this.dotnetInstance?.invokeMethodAsync("ReportProgress", initProgress);
    };

    // Initialize the engine
    public async initialize(selectedModels: string[], dotnet: DotNetInstance): Promise<void> {
        this.dotnetInstance = dotnet; // Store the .NET instance
        this.engine = await webllm.CreateMLCEngine(
            selectedModels,
            { initProgressCallback: this.initProgressCallback }, // engineConfig
        );
    }

    // Stream completion
    public async completeStream(messages: Message[], modelId: string, enableThinking: boolean, tools: Array<webllm.ChatCompletionTool>): Promise<void> {
        if (!this.engine) {
            throw new Error("Engine is not initialized.");
        }

        // Chunks is an AsyncGenerator object
        const chunks = await this.engine.chat.completions.create({
            messages,
            temperature: 0,
            seed: 42,
            model: modelId,
            tool_choice: "auto",
            tools: tools,
            stream: true, // Enable streaming
            stream_options: { include_usage: true },
            extra_body: {
                enable_thinking: enableThinking,
            },
        });

        for await (const chunk of chunks) {
            // Assuming chunk is of type Chunk (define below if needed)
            await this.dotnetInstance?.invokeMethodAsync("ReceiveChunkCompletion", chunk);
        }
    }
}

// Singleton instance of WebLLMInterop
const webLLMInteropInstance = new WebLLMInterop();

// Export the functions
export async function initializeWebLLM(selectedModels: string[], dotnet: DotNetInstance): Promise<void> {
    await webLLMInteropInstance.initialize(selectedModels, dotnet);
}

export async function completeStreamWebLLM(
    messages: Message[],
    modelId: string,
    enableThinking: boolean,
    tools: string
): Promise<void> {
    await webLLMInteropInstance.completeStream(messages, modelId, enableThinking, JSON.parse(tools));
}