import * as webllm from "https://esm.run/@mlc-ai/web-llm";

var engine: any; // <-- hold a reference to MLCEngine in the module
var dotnetInstance: any; // <-- hold a reference to the WebLLMService instance in the module

//const selectedModel = "Llama-3.2-1B-Instruct-q4f16_1-MLC";
const initProgressCallback = (initProgress: any) => {
    console.log(initProgress);
    dotnetInstance.invokeMethodAsync("OnInitializing", initProgress);
}

export async function initialize(selectedModel: string, dotnet: any) {
    dotnetInstance = dotnet; // <-- WebLLMService instance
    // const engine = await webllm.CreateMLCEngine(
    engine = await webllm.CreateMLCEngine(
        selectedModel,
        { initProgressCallback: initProgressCallback }, // engineConfig
    );
}

export async function completeStream(messages: any[]) {
    // Chunks is an AsyncGenerator object
    const chunks = await engine.chat.completions.create({
        messages,
        temperature: 1,
        stream: true, // <-- Enable streaming
        stream_options: { include_usage: true },
    });

    for await (const chunk of chunks) {
        //console.log(chunk);
        await dotnetInstance.invokeMethodAsync("ReceiveChunkCompletion", chunk);
    }
}
