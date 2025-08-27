var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __asyncValues = (this && this.__asyncValues) || function (o) {
    if (!Symbol.asyncIterator) throw new TypeError("Symbol.asyncIterator is not defined.");
    var m = o[Symbol.asyncIterator], i;
    return m ? m.call(o) : (o = typeof __values === "function" ? __values(o) : o[Symbol.iterator](), i = {}, verb("next"), verb("throw"), verb("return"), i[Symbol.asyncIterator] = function () { return this; }, i);
    function verb(n) { i[n] = o[n] && function (v) { return new Promise(function (resolve, reject) { v = o[n](v), settle(resolve, reject, v.done, v.value); }); }; }
    function settle(resolve, reject, d, v) { Promise.resolve(v).then(function(v) { resolve({ value: v, done: d }); }, reject); }
};
// Wllama Module
export class WllamaInterop {
    constructor() {
        this.modelPath = "";
    }
    // Initialize wllama with model download
    initialize(modelUrl, dotnet) {
        return __awaiter(this, void 0, void 0, function* () {
            var _a, _b, _c, _d, _e;
            this.dotnetInstance = dotnet;
            try {
                // Report initialization start
                yield ((_a = this.dotnetInstance) === null || _a === void 0 ? void 0 : _a.invokeMethodAsync("ReportProgress", {
                    progress: 0,
                    timeElapsed: 0,
                    text: "Initializing Wllama..."
                }));
                // Import wllama dynamically
                const wllamaModule = yield import('https://esm.run/@ngxson/wllama@1.10.0');
                // Create wllama instance
                const wllama = wllamaModule.wllama;
                this.wllamaInstance = new wllama();
                yield ((_b = this.dotnetInstance) === null || _b === void 0 ? void 0 : _b.invokeMethodAsync("ReportProgress", {
                    progress: 10,
                    timeElapsed: 0,
                    text: "Downloading model..."
                }));
                // Download model through proxy
                const modelResponse = yield fetch(`/api/proxy/download?url=${encodeURIComponent(modelUrl)}`);
                if (!modelResponse.ok) {
                    throw new Error(`Failed to download model: ${modelResponse.statusText}`);
                }
                const modelBuffer = yield modelResponse.arrayBuffer();
                yield ((_c = this.dotnetInstance) === null || _c === void 0 ? void 0 : _c.invokeMethodAsync("ReportProgress", {
                    progress: 70,
                    timeElapsed: 0,
                    text: "Loading model..."
                }));
                // Load model into wllama
                yield this.wllamaInstance.loadModel(new Uint8Array(modelBuffer));
                yield ((_d = this.dotnetInstance) === null || _d === void 0 ? void 0 : _d.invokeMethodAsync("ReportProgress", {
                    progress: 100,
                    timeElapsed: 0,
                    text: "Model loaded successfully"
                }));
            }
            catch (error) {
                console.error("Failed to initialize Wllama:", error);
                yield ((_e = this.dotnetInstance) === null || _e === void 0 ? void 0 : _e.invokeMethodAsync("ReportProgress", {
                    progress: 0,
                    timeElapsed: 0,
                    text: `Error: ${(error === null || error === void 0 ? void 0 : error.message) || 'Unknown error'}`
                }));
                throw error;
            }
        });
    }
    // Stream completion using wllama
    completeStream(messages, modelId, enableThinking, tools) {
        return __awaiter(this, void 0, void 0, function* () {
            var _a, e_1, _b, _c;
            var _d, _e, _f;
            if (!this.wllamaInstance) {
                throw new Error("Wllama is not initialized.");
            }
            try {
                // Convert messages to prompt format
                let prompt = this.convertMessagesToPrompt(messages, enableThinking);
                // Handle function calling if tools are provided
                if (tools && tools.length > 0) {
                    prompt = this.addToolsToPrompt(prompt, tools);
                }
                // Generate response with streaming
                const stream = this.wllamaInstance.createCompletion({
                    prompt: prompt,
                    temperature: 0.7,
                    top_p: 0.9,
                    max_tokens: 2048,
                    stream: true
                });
                try {
                    for (var _g = true, stream_1 = __asyncValues(stream), stream_1_1; stream_1_1 = yield stream_1.next(), _a = stream_1_1.done, !_a; _g = true) {
                        _c = stream_1_1.value;
                        _g = false;
                        const chunk = _c;
                        const wllamaChunk = {
                            choices: [{
                                    delta: {
                                        content: chunk.content || ""
                                    }
                                }],
                            done: chunk.done || false
                        };
                        yield ((_d = this.dotnetInstance) === null || _d === void 0 ? void 0 : _d.invokeMethodAsync("ReceiveChunkCompletion", {
                            choices: wllamaChunk.choices,
                            done: wllamaChunk.done
                        }));
                        if (chunk.done) {
                            break;
                        }
                    }
                }
                catch (e_1_1) { e_1 = { error: e_1_1 }; }
                finally {
                    try {
                        if (!_g && !_a && (_b = stream_1.return)) yield _b.call(stream_1);
                    }
                    finally { if (e_1) throw e_1.error; }
                }
                // Send final completion marker
                yield ((_e = this.dotnetInstance) === null || _e === void 0 ? void 0 : _e.invokeMethodAsync("ReceiveChunkCompletion", {
                    choices: [],
                    done: true
                }));
            }
            catch (error) {
                console.error("Error in Wllama completion:", error);
                yield ((_f = this.dotnetInstance) === null || _f === void 0 ? void 0 : _f.invokeMethodAsync("ReceiveChunkCompletion", {
                    choices: [{
                            delta: {
                                content: `Error: ${(error === null || error === void 0 ? void 0 : error.message) || 'Unknown error'}`
                            }
                        }],
                    done: true
                }));
            }
        });
    }
    convertMessagesToPrompt(messages, enableThinking) {
        let prompt = "";
        if (enableThinking) {
            prompt += "<|thinking|>\n";
        }
        for (const message of messages) {
            switch (message.role.toLowerCase()) {
                case "system":
                    prompt += `System: ${message.content}\n\n`;
                    break;
                case "user":
                    prompt += `Human: ${message.content}\n\n`;
                    break;
                case "assistant":
                    prompt += `Assistant: ${message.content}\n\n`;
                    break;
                default:
                    prompt += `${message.role}: ${message.content}\n\n`;
                    break;
            }
        }
        if (enableThinking) {
            prompt += "<|/thinking|>\n";
        }
        prompt += "Assistant: ";
        return prompt;
    }
    addToolsToPrompt(prompt, tools) {
        let toolsPrompt = "\n\nYou have access to the following functions:\n\n";
        for (const tool of tools) {
            if (tool.function) {
                toolsPrompt += `- ${tool.function.name}: ${tool.function.description || 'No description'}\n`;
                if (tool.function.parameters && tool.function.parameters.properties) {
                    toolsPrompt += "  Parameters:\n";
                    for (const [paramName, paramDef] of Object.entries(tool.function.parameters.properties)) {
                        const def = paramDef;
                        toolsPrompt += `    - ${paramName} (${def.type || 'string'}): ${def.description || 'No description'}\n`;
                    }
                }
                toolsPrompt += "\n";
            }
        }
        toolsPrompt += `
If you need to call a function, respond with a JSON object in this format:
{ "tool_calls": [
    {
        "id": "call_abc123",
        "type": "function",
        "function": {
            "name": "FunctionName",
            "arguments": "{\\"parameter1\\": \\"value\\", \\"parameter2\\": true}"
        }
    }
] }

`;
        return prompt + toolsPrompt;
    }
}
// Singleton instance of WllamaInterop
const wllamaInteropInstance = new WllamaInterop();
// Export the functions
export function initializeWllama(modelUrl, dotnet) {
    return __awaiter(this, void 0, void 0, function* () {
        yield wllamaInteropInstance.initialize(modelUrl, dotnet);
    });
}
export function completeStreamWllama(messages, modelId, enableThinking, tools) {
    return __awaiter(this, void 0, void 0, function* () {
        const parsedTools = tools ? JSON.parse(tools) : [];
        yield wllamaInteropInstance.completeStream(messages, modelId, enableThinking, parsedTools);
    });
}
