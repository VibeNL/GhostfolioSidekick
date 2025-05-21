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
    public async initialize(selectedModel: string, dotnet: DotNetInstance): Promise<void> {
        this.dotnetInstance = dotnet; // Store the .NET instance
        this.engine = await webllm.CreateMLCEngine(
            selectedModel,
            { initProgressCallback: this.initProgressCallback }, // engineConfig
            { context_window_size: 8096 } // modelConfig
        );
    }

    // Stream completion
    public async completeStream(enableThinking: boolean, messages: Message[]): Promise<void> {
        if (!this.engine) {
            throw new Error("Engine is not initialized.");
        }

        // Chunks is an AsyncGenerator object
        const chunks = await this.engine.chat.completions.create({
            messages,
            temperature: 0,
            seed: 42,
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
export async function initializeWebLLM(selectedModel: string, dotnet: DotNetInstance): Promise<void> {
    await webLLMInteropInstance.initialize(selectedModel, dotnet);
}

export async function completeStreamWebLLM(enableThinking: boolean,  messages: Message[]): Promise<void> {
    await webLLMInteropInstance.completeStream(enableThinking, messages);
}