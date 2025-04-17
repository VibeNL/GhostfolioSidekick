console.log("WebLLM loading!");

import * as webllm from "https://esm.run/@mlc-ai/web-llm";

console.log("WebLLM loaded successfully!");


// Callback function to update model loading progress
const initProgressCallback = (initProgress) => {
    console.log(initProgress);
}
const selectedModel = "Llama-3.2-1B-Instruct-q4f16_1-MLC";

const engine = await webllm.CreateMLCEngine(
    selectedModel,
    { initProgressCallback: initProgressCallback }, // engineConfig
);

// Basic example assuming webllm.js is loaded and model is initialized
window.webllmChat = async function (inputText) {
    debugger;
    
    if (!window.myWebLLM) {
        window.myWebLLM = await webllm.createChatWorker();
        await window.myWebLLM.load("Llama-3-8B-Instruct-q4f32_1");
    }
    const reply = await window.myWebLLM.chat.completion(inputText);
    return reply.message.content;
};
