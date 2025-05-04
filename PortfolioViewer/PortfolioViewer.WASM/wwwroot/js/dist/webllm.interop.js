"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
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
Object.defineProperty(exports, "__esModule", { value: true });
exports.WebLLMInterop = void 0;
exports.initializeWebLLM = initializeWebLLM;
exports.completeStreamWebLLM = completeStreamWebLLM;
const webllm = __importStar(require("@mlc-ai/web-llm"));
// WebLLM Module
class WebLLMInterop {
    constructor() {
        // Callback for initialization progress
        this.initProgressCallback = (initProgress) => {
            var _a;
            console.log(initProgress);
            (_a = this.dotnetInstance) === null || _a === void 0 ? void 0 : _a.invokeMethodAsync("ReportProgress", initProgress);
        };
    }
    // Initialize the engine
    initialize(selectedModel, dotnet) {
        return __awaiter(this, void 0, void 0, function* () {
            this.dotnetInstance = dotnet; // Store the .NET instance
            this.engine = yield webllm.CreateMLCEngine(selectedModel, { initProgressCallback: this.initProgressCallback }, // engineConfig
            { context_window_size: 8096 } // modelConfig
            );
        });
    }
    // Stream completion
    completeStream(messages) {
        return __awaiter(this, void 0, void 0, function* () {
            var _a, e_1, _b, _c;
            var _d;
            if (!this.engine) {
                throw new Error("Engine is not initialized.");
            }
            // Chunks is an AsyncGenerator object
            const chunks = yield this.engine.chat.completions.create({
                messages,
                temperature: 1,
                stream: true, // Enable streaming
                stream_options: { include_usage: true },
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
exports.WebLLMInterop = WebLLMInterop;
// Singleton instance of WebLLMInterop
const webLLMInteropInstance = new WebLLMInterop();
// Export the functions
function initializeWebLLM(selectedModel, dotnet) {
    return __awaiter(this, void 0, void 0, function* () {
        yield webLLMInteropInstance.initialize(selectedModel, dotnet);
    });
}
function completeStreamWebLLM(messages) {
    return __awaiter(this, void 0, void 0, function* () {
        yield webLLMInteropInstance.completeStream(messages);
    });
}
