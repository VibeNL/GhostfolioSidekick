declare module "https://esm.run/@mlc-ai/web-llm" {
    export interface MLCEngine {
        chat: {
            completions: {
                create(options: ChatCompletionRequest): AsyncGenerator<ChatCompletionChunk>;
            };
        };
    }

    export function CreateMLCEngine(
        model: string | string[],
        engineConfig: {
            initProgressCallback: (progress: any) => void,
        },
        modelConfig?: {
            sliding_window_size?: number;
            attention_sink_size?: number;
            context_window_size?: number;
        }
    ): Promise<MLCEngine>;

    interface Message {
        role: string;
        content: string;
    }

    interface ChatCompletionChunk {
        choices: any;
        usage?: any;
    }

    interface ChatCompletionRequest {
        messages: Message[];
        temperature: number;
        seed: number;
        model: string;
        stream: boolean;
        stream_options: { include_usage: boolean };
        tool_choice?: string;
        tools?: Array<ChatCompletionTool>;
        extra_body?: {
            enable_thinking?: boolean;
        };
    }

    interface ChatCompletionTool {
        function: FunctionDefinition;
        type: "function";
    }

    interface FunctionDefinition {
        name: string;
        description?: string;
        parameters?: FunctionParameters;
    }

    type FunctionParameters = Record<string, unknown>;
}