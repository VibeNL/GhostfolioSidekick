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
        this.isWebGPUSupported = false;
        this.initializationError = null;
        // Callback for initialization progress
        this.initProgressCallback = (initProgress) => {
            var _a;
            console.log(initProgress);
            (_a = this.dotnetInstance) === null || _a === void 0 ? void 0 : _a.invokeMethodAsync("ReportProgress", initProgress);
        };
        // Callback for initialization errors
        this.reportError = (error) => {
            var _a;
            console.error("WebLLM Error:", error);
            (_a = this.dotnetInstance) === null || _a === void 0 ? void 0 : _a.invokeMethodAsync("ReportError", error);
        };
    }
    // Check WebGPU support
    checkWebGPUSupport() {
        return __awaiter(this, void 0, void 0, function* () {
            try {
                if (!navigator.gpu) {
                    console.warn("WebGPU is not supported in this browser");
                    return false;
                }
                const adapter = yield navigator.gpu.requestAdapter();
                if (!adapter) {
                    console.warn("No WebGPU adapter found");
                    return false;
                }
                console.log("WebGPU is supported and adapter found");
                return true;
            }
            catch (error) {
                console.error("Error checking WebGPU support:", error);
                return false;
            }
        });
    }
    // Initialize the engine with comprehensive error handling
    initialize(selectedModels, dotnet) {
        return __awaiter(this, void 0, void 0, function* () {
            var _a, _b, _c, _d;
            this.dotnetInstance = dotnet; // Store the .NET instance
            try {
                // Check WebGPU support first
                this.isWebGPUSupported = yield this.checkWebGPUSupport();
                if (!this.isWebGPUSupported) {
                    const errorReport = {
                        errorType: "WebGPU_Not_Supported",
                        errorMessage: "WebGPU is not supported or available in this browser. WebLLM requires WebGPU to run AI models locally.",
                        isRecoverable: false,
                        suggestions: [
                            "Use a WebGPU-compatible browser (Chrome 113+, Edge 113+, Firefox with experimental features enabled)",
                            "Ensure your GPU drivers are up to date",
                            "Check if WebGPU is enabled in your browser settings",
                            "Visit https://webgpureport.org/ to check your browser's WebGPU compatibility",
                            "Consider using a cloud-based AI service as a fallback"
                        ]
                    };
                    this.reportError(errorReport);
                    this.initializationError = errorReport.errorMessage;
                    return;
                }
                // Report that WebGPU is available
                this.initProgressCallback({
                    progress: 0.1,
                    timeElapsed: 0,
                    text: "WebGPU support detected, initializing WebLLM..."
                });
                // Initialize the WebLLM engine
                this.engine = yield webllm.CreateMLCEngine(selectedModels, { initProgressCallback: this.initProgressCallback });
                console.log("WebLLM engine initialized successfully");
            }
            catch (error) {
                console.error("Failed to initialize WebLLM:", error);
                let errorReport;
                if (((_a = error.message) === null || _a === void 0 ? void 0 : _a.includes("GPU")) || ((_b = error.message) === null || _b === void 0 ? void 0 : _b.includes("WebGPU"))) {
                    errorReport = {
                        errorType: "GPU_Initialization_Failed",
                        errorMessage: `GPU initialization failed: ${error.message}`,
                        isRecoverable: false,
                        suggestions: [
                            "Check if your GPU supports WebGPU",
                            "Update your GPU drivers",
                            "Try using a different browser",
                            "Restart your browser and try again",
                            "Check browser console for additional error details"
                        ]
                    };
                }
                else if (((_c = error.message) === null || _c === void 0 ? void 0 : _c.includes("network")) || ((_d = error.message) === null || _d === void 0 ? void 0 : _d.includes("fetch"))) {
                    errorReport = {
                        errorType: "Network_Error",
                        errorMessage: `Network error during initialization: ${error.message}`,
                        isRecoverable: true,
                        suggestions: [
                            "Check your internet connection",
                            "Try again in a few moments",
                            "Ensure the model files can be downloaded"
                        ]
                    };
                }
                else {
                    errorReport = {
                        errorType: "Unknown_Error",
                        errorMessage: `Unknown error during initialization: ${error.message}`,
                        isRecoverable: false,
                        suggestions: [
                            "Check browser console for detailed error information",
                            "Try refreshing the page",
                            "Use a different browser or device"
                        ]
                    };
                }
                this.reportError(errorReport);
                this.initializationError = errorReport.errorMessage;
            }
        });
    }
    // Stream completion with error handling
    completeStream(messages, modelId, enableThinking, tools) {
        return __awaiter(this, void 0, void 0, function* () {
            var _a, e_1, _b, _c;
            var _d;
            try {
                if (this.initializationError) {
                    throw new Error(`Cannot complete stream: ${this.initializationError}`);
                }
                if (!this.engine) {
                    throw new Error("Engine is not initialized. Please initialize WebLLM first.");
                }
                if (!this.isWebGPUSupported) {
                    throw new Error("WebGPU is not supported. Cannot process completion requests.");
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
                        try {
                            yield ((_d = this.dotnetInstance) === null || _d === void 0 ? void 0 : _d.invokeMethodAsync("ReceiveChunkCompletion", chunk));
                        }
                        catch (chunkError) {
                            console.error("Error processing chunk:", chunkError);
                            // Continue processing other chunks
                        }
                    }
                }
                catch (e_1_1) { e_1 = { error: e_1_1 }; }
                finally {
                    try {
                        if (!_e && !_a && (_b = chunks_1.return)) yield _b.call(chunks_1);
                    }
                    finally { if (e_1) throw e_1.error; }
                }
            }
            catch (error) {
                console.error("Error in completeStream:", error);
                const errorReport = {
                    errorType: "Completion_Error",
                    errorMessage: `Error during completion: ${error.message}`,
                    isRecoverable: true,
                    suggestions: [
                        "Try the request again",
                        "Check if the model is properly loaded",
                        "Verify the input messages are valid"
                    ]
                };
                this.reportError(errorReport);
                throw error; // Re-throw to maintain existing error handling behavior
            }
        });
    }
    // Check if WebLLM is ready to use
    isReady() {
        return this.engine !== undefined && this.isWebGPUSupported && !this.initializationError;
    }
    // Get initialization status
    getStatus() {
        return {
            ready: this.isReady(),
            error: this.initializationError,
            webGPUSupported: this.isWebGPUSupported
        };
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
        const parsedTools = tools ? JSON.parse(tools) : [];
        yield webLLMInteropInstance.completeStream(messages, modelId, enableThinking, parsedTools);
    });
}
// Export status checking functions
export function isWebLLMReady() {
    return webLLMInteropInstance.isReady();
}
export function getWebLLMStatus() {
    return webLLMInteropInstance.getStatus();
}
// Export WebGPU check function for standalone use
export function checkWebGPUSupport() {
    return __awaiter(this, void 0, void 0, function* () {
        try {
            if (!navigator.gpu) {
                return false;
            }
            const adapter = yield navigator.gpu.requestAdapter();
            return adapter !== null;
        }
        catch (error) {
            console.error("Error checking WebGPU support:", error);
            return false;
        }
    });
}
