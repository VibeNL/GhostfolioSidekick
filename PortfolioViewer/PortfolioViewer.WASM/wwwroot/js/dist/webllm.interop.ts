import * as webllm from "https://esm.run/@mlc-ai/web-llm";

// Define types for the module
export interface DotNetInstance {
    invokeMethodAsync(methodName: string, ...args: any[]): Promise<void>;
}

export interface Message {
    role: string;
    content: string;
}

export interface InitProgressReport {
    progress: number;
    timeElapsed: number;
    text: string;
}

export interface ErrorReport {
    errorType: string;
    errorMessage: string;
    isRecoverable: boolean;
    suggestions: string[];
}

// WebGPU types for browsers that support it
declare global {
    interface Navigator {
        gpu?: {
            requestAdapter(): Promise<any>;
        };
    }
}

// WebLLM Module
export class WebLLMInterop {
    private engine: webllm.MLCEngine | undefined;
    private dotnetInstance: DotNetInstance | undefined;
    private isWebGPUSupported: boolean = false;
    private initializationError: string | null = null;

    constructor() { }

    // Check WebGPU support
    private async checkWebGPUSupport(): Promise<boolean> {
        try {
            if (!(navigator as any).gpu) {
                console.warn("WebGPU is not supported in this browser");
                return false;
            }

            const adapter = await (navigator as any).gpu.requestAdapter();
            if (!adapter) {
                console.warn("No WebGPU adapter found");
                return false;
            }

            console.log("WebGPU is supported and adapter found");
            return true;
        } catch (error) {
            console.error("Error checking WebGPU support:", error);
            return false;
        }
    }

    // Callback for initialization progress
    private initProgressCallback = (initProgress: InitProgressReport): void => {
        console.log(initProgress);
        this.dotnetInstance?.invokeMethodAsync("ReportProgress", initProgress);
    };

    // Callback for initialization errors
    private reportError = (error: ErrorReport): void => {
        console.error("WebLLM Error:", error);
        this.dotnetInstance?.invokeMethodAsync("ReportError", error);
    };

    // Initialize the engine with comprehensive error handling
    public async initialize(selectedModels: string[], dotnet: DotNetInstance): Promise<void> {
        this.dotnetInstance = dotnet; // Store the .NET instance
        
        try {
            // Check WebGPU support first
            this.isWebGPUSupported = await this.checkWebGPUSupport();
            
            if (!this.isWebGPUSupported) {
                const errorReport: ErrorReport = {
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
            this.engine = await webllm.CreateMLCEngine(
                selectedModels,
                { initProgressCallback: this.initProgressCallback }
            );

            console.log("WebLLM engine initialized successfully");

        } catch (error: any) {
            console.error("Failed to initialize WebLLM:", error);
            
            let errorReport: ErrorReport;
            
            if (error.message?.includes("GPU") || error.message?.includes("WebGPU")) {
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
            } else if (error.message?.includes("network") || error.message?.includes("fetch")) {
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
            } else {
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
    }

    // Stream completion with error handling
    public async completeStream(messages: Message[], modelId: string, enableThinking: boolean, tools: Array<webllm.ChatCompletionTool>): Promise<void> {
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
            const chunks = await this.engine.chat.completions.create({
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

            for await (const chunk of chunks) {
                try {
                    await this.dotnetInstance?.invokeMethodAsync("ReceiveChunkCompletion", chunk);
                } catch (chunkError: any) {
                    console.error("Error processing chunk:", chunkError);
                    // Continue processing other chunks
                }
            }

        } catch (error: any) {
            console.error("Error in completeStream:", error);
            
            const errorReport: ErrorReport = {
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
    }

    // Check if WebLLM is ready to use
    public isReady(): boolean {
        return this.engine !== undefined && this.isWebGPUSupported && !this.initializationError;
    }

    // Get initialization status
    public getStatus(): { ready: boolean; error: string | null; webGPUSupported: boolean } {
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
export async function initializeWebLLM(selectedModels: string[], dotnet: DotNetInstance): Promise<void> {
    await webLLMInteropInstance.initialize(selectedModels, dotnet);
}

export async function completeStreamWebLLM(
    messages: Message[],
    modelId: string,
    enableThinking: boolean,
    tools: string
): Promise<void> {
    const parsedTools = tools ? JSON.parse(tools) : [];
    await webLLMInteropInstance.completeStream(messages, modelId, enableThinking, parsedTools);
}

// Export status checking functions
export function isWebLLMReady(): boolean {
    return webLLMInteropInstance.isReady();
}

export function getWebLLMStatus(): { ready: boolean; error: string | null; webGPUSupported: boolean } {
    return webLLMInteropInstance.getStatus();
}

// Export WebGPU check function for standalone use
export async function checkWebGPUSupport(): Promise<boolean> {
    try {
        if (!(navigator as any).gpu) {
            return false;
        }
        const adapter = await (navigator as any).gpu.requestAdapter();
        return adapter !== null;
    } catch (error) {
        console.error("Error checking WebGPU support:", error);
        return false;
    }
}