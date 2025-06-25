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
import * as webllm from "https://esm.run/@mlc-ai/web-llm";
// WebLLM Module
export class WebLLMInterop {
    constructor() {
        // Callback for initialization progress
        this.initProgressCallback = (initProgress) => {
            var _a;
            console.log(initProgress);
            (_a = this.dotnetInstance) === null || _a === void 0 ? void 0 : _a.invokeMethodAsync("ReportProgress", initProgress);
        };
    }
    // Initialize the engine
    initialize(selectedModels, dotnet) {
        return __awaiter(this, void 0, void 0, function* () {
            this.dotnetInstance = dotnet; // Store the .NET instance
            this.engine = yield webllm.CreateMLCEngine(selectedModels, { initProgressCallback: this.initProgressCallback });
        });
    }
    // Stream completion
    completeStream(messages, modelId, enableThinking, tools) {
        return __awaiter(this, void 0, void 0, function* () {
            var _a, e_1, _b, _c;
            var _d;
            if (!this.engine) {
                throw new Error("Engine is not initialized.");
            }
            // Chunks is an AsyncGenerator object
            const chunks = yield this.engine.chat.completions.create({
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
            try {
                for (var _e = true, chunks_1 = __asyncValues(chunks), chunks_1_1; chunks_1_1 = yield chunks_1.next(), _a = chunks_1_1.done, !_a; _e = true) {
                    _c = chunks_1_1.value;
                    _e = false;
                    const chunk = _c;
                    // Assuming chunk is of type Chunk (define below if needed)
                    yield ((_d = this.dotnetInstance) === null || _d === void 0 ? void 0 : _d.invokeMethodAsync("ReceiveChunkCompletion", chunk));
                }
            }
            catch (e_1_1) { e_1 = { error: e_1_1 }; }
            finally {
                try {
                    if (!_e && !_a && (_b = chunks_1.return)) yield _b.call(chunks_1);
                }
                finally { if (e_1) throw e_1.error; }
            }
        });
    }
}
// Singleton instance of WebLLMInterop
const webLLMInteropInstance = new WebLLMInterop();
// Export the functions
export function initializeWebLLM(selectedModels, dotnet) {
    return __awaiter(this, void 0, void 0, function* () {
        yield webLLMInteropInstance.initialize(selectedModels, dotnet);
    });
}
export function completeStreamWebLLM(messages, modelId, enableThinking, tools) {
    return __awaiter(this, void 0, void 0, function* () {
        yield webLLMInteropInstance.completeStream(messages, modelId, enableThinking, JSON.parse(tools));
    });
}
