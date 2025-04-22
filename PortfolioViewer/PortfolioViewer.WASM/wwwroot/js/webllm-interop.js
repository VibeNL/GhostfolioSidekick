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
        engine = yield webllm.CreateMLCEngine(selectedModel, { initProgressCallback: initProgressCallback } // engineConfig
        );
    });
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
export function completeStream(messages) {
    return __awaiter(this, void 0, void 0, function* () {
        var _a, e_1, _b, _c;
        var _d, _e;
        if (!engine) {
            throw new Error("Engine is not initialized.");
        }
        // Chunks is an AsyncGenerator object
        var request = {
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
        const asyncChunkGenerator = yield engine.chat.completions.create(request);
        let message = "";
        let lastChunk;
        let usageChunk;
        try {
            for (var _f = true, asyncChunkGenerator_1 = __asyncValues(asyncChunkGenerator), asyncChunkGenerator_1_1; asyncChunkGenerator_1_1 = yield asyncChunkGenerator_1.next(), _a = asyncChunkGenerator_1_1.done, !_a; _f = true) {
                _c = asyncChunkGenerator_1_1.value;
                _f = false;
                const chunk = _c;
                console.log(chunk);
                message += ((_e = (_d = chunk.choices[0]) === null || _d === void 0 ? void 0 : _d.delta) === null || _e === void 0 ? void 0 : _e.content) || "";
                if (!chunk.usage) {
                    lastChunk = chunk;
                }
                usageChunk = chunk;
                yield (dotnetInstance === null || dotnetInstance === void 0 ? void 0 : dotnetInstance.invokeMethodAsync("ReceiveChunkCompletion", chunk));
            }
        }
        catch (e_1_1) { e_1 = { error: e_1_1 }; }
        finally {
            try {
                if (!_f && !_a && (_b = asyncChunkGenerator_1.return)) yield _b.call(asyncChunkGenerator_1);
            }
            finally { if (e_1) throw e_1.error; }
        }
        console.log(lastChunk.choices[0].delta);
        console.log(usageChunk.usage);
    });
}
