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
// Define types for engine and dotnetInstance
let engine; // Assuming MLCEngine is a type from the webllm library
let dotnetInstance; // Define DotNetInstance type below
// Callback for initialization progress
const initProgressCallback = (initProgress) => {
    console.log(initProgress);
    dotnetInstance === null || dotnetInstance === void 0 ? void 0 : dotnetInstance.invokeMethodAsync("OnInitializing", initProgress);
};
// Initialize the engine
export function initialize(selectedModel, dotnet) {
    return __awaiter(this, void 0, void 0, function* () {
        dotnetInstance = dotnet; // Store the .NET instance
        engine = yield webllm.CreateMLCEngine(selectedModel, { initProgressCallback: initProgressCallback }, // engineConfig
        { context_window_size: 8096 } // modelConfig
        );
    });
}
export function completeStream(messages) {
    return __awaiter(this, void 0, void 0, function* () {
        var _a, e_1, _b, _c;
        if (!engine) {
            throw new Error("Engine is not initialized.");
        }
        // Chunks is an AsyncGenerator object
        const chunks = yield engine.chat.completions.create({
            messages,
            temperature: 1,
            stream: true, // Enable streaming
            stream_options: { include_usage: true },
        });
        try {
            for (var _d = true, chunks_1 = __asyncValues(chunks), chunks_1_1; chunks_1_1 = yield chunks_1.next(), _a = chunks_1_1.done, !_a; _d = true) {
                _c = chunks_1_1.value;
                _d = false;
                const chunk = _c;
                // Assuming chunk is of type Chunk (define below if needed)
                yield (dotnetInstance === null || dotnetInstance === void 0 ? void 0 : dotnetInstance.invokeMethodAsync("ReceiveChunkCompletion", chunk));
            }
        }
        catch (e_1_1) { e_1 = { error: e_1_1 }; }
        finally {
            try {
                if (!_d && !_a && (_b = chunks_1.return)) yield _b.call(chunks_1);
            }
            finally { if (e_1) throw e_1.error; }
        }
    });
}
