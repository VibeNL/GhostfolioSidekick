// Basic example assuming webllm.js is loaded and model is initialized
window.webllmChat = async function (inputText) {
    if (!window.myWebLLM) {
        window.myWebLLM = await webllm.createChatWorker();
        await window.myWebLLM.load("Llama-3-8B-Instruct-q4f32_1");
    }
    const reply = await window.myWebLLM.chat.completion(inputText);
    return reply.message.content;
};
