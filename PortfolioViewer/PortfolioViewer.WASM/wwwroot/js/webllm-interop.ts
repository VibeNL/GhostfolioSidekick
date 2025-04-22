import * as webllm from "https://esm.run/@mlc-ai/web-llm";

// Define types for engine and dotnetInstance
let engine: webllm.MLCEngine | undefined; // Assuming MLCEngine is a type from the webllm library
let dotnetInstance: DotNetInstance | undefined; // Define DotNetInstance type below

// Define the type for the dotnetInstance
interface DotNetInstance {
    invokeMethodAsync(methodName: string, ...args: any[]): Promise<void>;
}

// Define the type for the initProgressCallback parameter
type InitProgress = any; // Replace `any` with the actual type if known

// Callback for initialization progress
const initProgressCallback = (initProgress: InitProgress): void => {
    console.log(initProgress);
    dotnetInstance?.invokeMethodAsync("OnInitializing", initProgress);
};

// Initialize the engine
export async function initialize(selectedModel: string, dotnet: DotNetInstance): Promise<void> {
    dotnetInstance = dotnet; // Store the .NET instance
    engine = await webllm.CreateMLCEngine(
        selectedModel,
        { initProgressCallback: initProgressCallback } // engineConfig
    );
}

// Define the type for the messages parameter in completeStream
interface Message {
    role: string;
    content: string;
}

export async function completeStream(messages: Message[]): Promise<void> {
    if (!engine) {
        throw new Error("Engine is not initialized.");
    }

    // Chunks is an AsyncGenerator object
    const chunks = await engine.chat.completions.create({
        messages,
        temperature: 1,
        stream: true, // Enable streaming
        stream_options: { include_usage: true },
    });

    for await (const chunk of chunks) {
        // Assuming chunk is of type Chunk (define below if needed)
        await dotnetInstance?.invokeMethodAsync("ReceiveChunkCompletion", chunk);
    }
}