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

//const tools: Array<webllm.ChatCompletionTool> = [
//    {
//        type: "function",
//        function: {
//            name: "get_current_weather",
//            description: "Get the current weather in a given location",
//            parameters: {
//                type: "object",
//                properties: {
//                    location: {
//                        type: "string",
//                        description: "The city and state, e.g. San Francisco, CA",
//                    },
//                    unit: { type: "string", enum: ["celsius", "fahrenheit"] },
//                },
//                required: ["location"],
//            },
//        },
//    },
//];

export async function completeStream(messages: Message[]): Promise<void> {
    if (!engine) {
        throw new Error("Engine is not initialized.");
    }

    // Chunks is an AsyncGenerator object
    var request: webllm.ChatCompletionRequest = {
        messages,
        temperature: 1,
        stream: true, // Enable streaming
        stream_options: { include_usage: true }
        //tool_choice: "auto",
        //tools: tools,
    };

    /*for await (const chunk of chunks) {
        // Assuming chunk is of type Chunk (define below if needed)
        await dotnetInstance?.invokeMethodAsync("ReceiveChunkCompletion", chunk);
    }*/

    const asyncChunkGenerator = await engine.chat.completions.create(request);
    let message = "";
    let lastChunk: webllm.ChatCompletionChunk | undefined;
    let usageChunk: webllm.ChatCompletionChunk | undefined;
    for await (const chunk of asyncChunkGenerator) {
        console.log(chunk);
        message += chunk.choices[0]?.delta?.content || "";
        if (!chunk.usage) {
            lastChunk = chunk;
        }
        usageChunk = chunk;

        await dotnetInstance?.invokeMethodAsync("ReceiveChunkCompletion", chunk);
    }
    console.log(lastChunk!.choices[0].delta);
    console.log(usageChunk!.usage);
}
