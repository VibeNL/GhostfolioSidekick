declare module "https://esm.run/@mlc-ai/web-llm" {
    export interface MLCEngine
{
	chat: {
            completions: {
                create(options: {
	messages: Message[];
	temperature: number;
	stream: boolean;
	stream_options: { include_usage: boolean }
		;
	}): AsyncGenerator<Chunk>;
            };
        };
    }

    export function CreateMLCEngine(
        model: string,
		config: { initProgressCallback: (progress: any) => void }
    ): Promise<MLCEngine>;

interface Message
{
	role: string;
        content: string;
    }

interface Chunk
{
	// Define the structure of a chunk if known
}
}
