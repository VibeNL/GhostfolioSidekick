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

// WebLLM Module
export class WebLLMInterop {
	private engine: webllm.MLCEngine | undefined;
	private dotnetInstance: DotNetInstance | undefined;

	constructor() { }

	private static async ensureWebGpu(): Promise<void> {
		const gpu = (navigator as any).gpu;
		if (!gpu) {
			throw new Error("WebGPU is not available in this browser. Use a recent Chrome/Edge (or Safari 26+) with WebGPU enabled.");
		}
		if (!(await gpu.requestAdapter())) {
			throw new Error("No WebGPU adapter found. This device cannot run the on-device AI model.");
		}
	}

	private static createWorker(): Worker {
		const code = `import { WebWorkerMLCEngineHandler } from "https://esm.run/@mlc-ai/web-llm";
const handler = new WebWorkerMLCEngineHandler();
self.onmessage = (msg) => handler.onmessage(msg);`;
		return new Worker(URL.createObjectURL(new Blob([code], { type: "text/javascript" })), { type: "module" });
	}

	// Callback for initialization progress
	private initProgressCallback = (initProgress: InitProgressReport): void => {
		console.log(initProgress);
		this.dotnetInstance?.invokeMethodAsync("ReportProgress", initProgress);
	};

	// Initialize the engine
	public async initialize(selectedModels: string[], dotnet: DotNetInstance): Promise<void> {
		this.dotnetInstance = dotnet; // Store the .NET instance
		if (this.engine) {
			return; // reuse the already loaded engine
		}

		await WebLLMInterop.ensureWebGpu();
		const models = selectedModels;
		const config = { initProgressCallback: this.initProgressCallback };

		// Run the model in a worker so the UI thread stays responsive
		this.engine = typeof Worker !== "undefined"
			? await webllm.CreateWebWorkerMLCEngine(WebLLMInterop.createWorker(), models, config, { context_window_size: 4096 })
			: await webllm.CreateMLCEngine(models, config);
	}

	// Stream completion
	public async completeStream(messages: Message[], modelId: string): Promise<void> {
		if (!this.engine) {
			throw new Error("Engine is not initialized.");
		}

		try {
			const chunks = await this.engine.chat.completions.create({
				messages,
				temperature: 0,
				seed: 42,
				max_tokens: 1024,
				model: modelId,
				tool_choice: "auto",
				tools: undefined,
				stream: true, // Enable streaming
				stream_options: { include_usage: true },
				extra_body: {
					enable_thinking: true, // always include thinking in the response
				},
			});

			for await (const chunk of chunks) {
				// Assuming chunk is of type Chunk (define below if needed)
				await this.dotnetInstance?.invokeMethodAsync("ReceiveChunkCompletion", chunk);
			}
		} catch (error) {
			console.error("Error during streaming completion:", error);
			// Always finish the stream so the .NET side never waits forever
			await this.dotnetInstance?.invokeMethodAsync("ReceiveChunkCompletion", {
				id: "error", object: "error", model: modelId, system_fingerprint: "",
				choices: [{ index: 0, delta: { role: "assistant", content: `The AI model failed to respond: ${error}` }, logprobs: "", finish_reason: "error" }],
			});
			await this.dotnetInstance?.invokeMethodAsync("ReceiveChunkCompletion", {
				id: "error", object: "error", model: modelId, system_fingerprint: "", choices: [],
				usage: { completion_tokens: 0, prompt_tokens: 0, total_tokens: 0 },
			});
		}
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
	modelId: string
): Promise<void> {
	await webLLMInteropInstance.completeStream(messages, modelId);
}