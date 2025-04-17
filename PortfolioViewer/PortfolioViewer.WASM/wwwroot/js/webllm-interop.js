import * as webllm from "https://esm.run/@mlc-ai/web-llm";

var engine; // <-- hold a reference to MLCEngine in the module
var dotnetInstance; // <-- hold a reference to the WebLLMService instance in the module

//const selectedModel = "Llama-3.2-1B-Instruct-q4f16_1-MLC";
const initProgressCallback = (initProgress) => {
    console.log(initProgress);
    dotnetInstance.invokeMethodAsync("OnInitializing", initProgress);
}

export async function initialize(selectedModel, dotnet) {
       dotnetInstance = dotnet; // <-- WebLLMService innstance
     // const engine = await webllm.CreateMLCEngine(
        engine = await webllm.CreateMLCEngine(
            selectedModel,
            { initProgressCallback: initProgressCallback }, // engineConfig
        );
}