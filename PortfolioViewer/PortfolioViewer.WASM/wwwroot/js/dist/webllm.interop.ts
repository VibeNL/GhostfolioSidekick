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
    public async completeStream(messages: Message[]): Promise<void> {
        if (!this.engine) {
            throw new Error("Engine is not initialized.");
        }

        // Chunks is an AsyncGenerator object
        const chunks = await this.engine.chat.completions.create({
            messages,
            temperature: 1,
            stream: true, // Enable streaming
            stream_options: { include_usage: true },
        });

        for await (const chunk of chunks) {
            // Assuming chunk is of type Chunk (define below if needed)
            await this.dotnetInstance?.invokeMethodAsync("ReceiveChunkCompletion", chunk);
        }
    }
}

// Export a function to initialize WebLLMInterop
export async function initializeWebLLM(selectedModel: string, dotnet: DotNetInstance): Promise<void> {
    const interop = new WebLLMInterop();
    await interop.initialize(selectedModel, dotnet);
}