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
	// Serializes streams so a new request never starts while the previous one's internal GPU buffer cleanup is still running (mlc-ai/web-llm#497).
	private streamChain: Promise<void> = Promise.resolve();

	constructor() { }

	// Callback for initialization progress
	private initProgressCallback = (initProgress: InitProgressReport): void => {
		console.log(initProgress);
		this.dotnetInstance?.invokeMethodAsync("ReportProgress", initProgress);
	};

	// Initialize the engine
	public async initialize(selectedModels: string[], dotnet: DotNetInstance): Promise<void> {
		this.dotnetInstance = dotnet; // Store the .NET instance
		if (this.engine) {
			// Release GPU buffers of a previous (possibly corrupted) engine so re-initialization starts clean.
			try {
				await this.engine.unload();
			} catch (error) {
				console.warn("Failed to unload previous WebLLM engine:", error);
			}
			this.engine = undefined;
		}
		this.engine = await webllm.CreateMLCEngine(
			selectedModels,
			{ initProgressCallback: this.initProgressCallback }, // engineConfig
		);
	}

	// Stream completion (serialized via streamChain — see field comment)
	public completeStream(messages: Message[], modelId: string): Promise<void> {
		const run = this.streamChain.then(() => this.runCompletion(messages, modelId));
		this.streamChain = run.catch(() => undefined);
		return run;
	}

	private async runCompletion(messages: Message[], modelId: string): Promise<void> {
		if (!this.engine) {
			throw new Error("Engine is not initialized.");
		}

		try {
			const chunks = await this.engine.chat.completions.create({
				messages,
				temperature: 0,
				seed: 42,
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
			throw error;
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