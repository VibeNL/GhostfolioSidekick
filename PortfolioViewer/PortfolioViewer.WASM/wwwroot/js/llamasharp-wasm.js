// LLamaSharp WASM Backend JavaScript Interface
// This file provides JavaScript bindings for LLamaSharp WebAssembly operations

window.llamaSharpWasm = {
    // State management
    _backendLoaded: false,
    wasmModule: null,
    activeModels: new Map(),
    streamingSessions: new Map(),
    
    // Check if the WASM backend is loaded
    isBackendLoaded: function() {
        return this._backendLoaded;
    },

    // Load the LLamaSharp WASM backend
    loadBackend: async function() {
        try {
            console.log('Loading LLamaSharp WASM backend...');
            
            // Check if we already have a WASM module loaded
            if (this.wasmModule) {
                this._backendLoaded = true;
                return true;
            }

            // For now, we'll simulate loading a WASM backend
            // In a real implementation, you would load the actual llama.cpp WASM binary
            
            // Simulate async loading
            await new Promise(resolve => setTimeout(resolve, 1000));
            
            // Create a mock WASM module interface
            this.wasmModule = {
                _malloc: function(size) { return new ArrayBuffer(size); },
                _free: function(ptr) { /* no-op for simulation */ },
                _llamacpp_load_model: function(modelData, params) { 
                    console.log('Mock: Loading model with params:', params);
                    return 1; // Mock model handle
                },
                _llamacpp_generate: function(handle, prompt, callback) {
                    // Mock text generation
                    const responses = [
                        "I understand you're asking about ",
                        "portfolio management. ",
                        "This is a complex topic that involves ",
                        "balancing risk and return ",
                        "across different asset classes."
                    ];
                    
                    let i = 0;
                    const interval = setInterval(() => {
                        if (i < responses.length) {
                            callback(responses[i], false);
                            i++;
                        } else {
                            callback('', true); // Signal completion
                            clearInterval(interval);
                        }
                    }, 200);
                    
                    return interval; // Return handle for stopping
                }
            };
            
            this._backendLoaded = true;
            console.log('LLamaSharp WASM backend loaded successfully');
            return true;
            
        } catch (error) {
            console.error('Failed to load LLamaSharp WASM backend:', error);
            this._backendLoaded = false;
            throw error;
        }
    },

    // Initialize a model in the WASM environment
    initializeModel: async function(modelPath, params) {
        try {
            if (!this._backendLoaded) {
                throw new Error('WASM backend not loaded');
            }

            console.log('Initializing model:', modelPath, 'with params:', params);
            
            // Check if blazorBrowserStorage is available
            if (!window.blazorBrowserStorage) {
                throw new Error('blazorBrowserStorage not available');
            }
            
            // Extract model ID from path
            const modelId = modelPath.split('/').pop();
            if (!modelId) {
                throw new Error('Invalid model path: ' + modelPath);
            }
            
            // Check if model exists first
            const modelExists = await blazorBrowserStorage.hasModel(modelId);
            if (!modelExists) {
                throw new Error('Model not found in browser storage: ' + modelId);
            }
            
            // Get model data from IndexedDB (previously downloaded)
            const modelData = await blazorBrowserStorage.getModelData(modelId);
            
            if (!modelData) {
                throw new Error('Model data not found in browser storage: ' + modelId);
            }
            
            console.log('Retrieved model data, size:', modelData.byteLength, 'bytes');

            // Initialize the model in WASM
            const modelHandle = this.wasmModule._llamacpp_load_model(modelData, params);
            
            if (modelHandle <= 0) {
                throw new Error('Failed to load model in WASM');
            }

            // Store the model handle
            this.activeModels.set(modelPath, {
                handle: modelHandle,
                params: params,
                isReady: true
            });

            console.log('Model initialized successfully with handle:', modelHandle);
            return true;
            
        } catch (error) {
            console.error('Failed to initialize model:', error);
            return false;
        }
    },

    // Start a streaming generation session
    startStreaming: async function(prompt, options) {
        try {
            if (!this._backendLoaded) {
                throw new Error('WASM backend not loaded');
            }

            const streamId = 'stream_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
            
            // Find an active model (use the first one for simplicity)
            const modelEntry = Array.from(this.activeModels.values())[0];
            if (!modelEntry || !modelEntry.isReady) {
                throw new Error('No ready model found');
            }

            // Create streaming session
            const session = {
                id: streamId,
                modelHandle: modelEntry.handle,
                prompt: prompt,
                options: options,
                results: [],
                isComplete: false,
                hasError: false,
                errorMessage: ''
            };

            this.streamingSessions.set(streamId, session);

            // Start the generation process
            const generationHandle = this.wasmModule._llamacpp_generate(
                modelEntry.handle,
                prompt,
                (text, isComplete) => {
                    session.results.push(text);
                    session.isComplete = isComplete;
                    
                    if (isComplete) {
                        console.log('Generation completed for session:', streamId);
                    }
                }
            );

            session.generationHandle = generationHandle;
            
            console.log('Started streaming session:', streamId);
            return streamId;
            
        } catch (error) {
            console.error('Failed to start streaming:', error);
            throw error;
        }
    },

    // Get streaming results
    getStreamingResult: function(streamId) {
        const session = this.streamingSessions.get(streamId);
        if (!session) {
            return {
                text: '',
                isComplete: true,
                hasError: true,
                errorMessage: 'Session not found'
            };
        }

        // Get new text since last call
        const newText = session.results.join('');
        session.results = []; // Clear consumed results

        return {
            text: newText,
            isComplete: session.isComplete,
            hasError: session.hasError,
            errorMessage: session.errorMessage
        };
    },

    // Cleanup streaming session
    stopStreaming: function(streamId) {
        const session = this.streamingSessions.get(streamId);
        if (session && session.generationHandle) {
            // Stop the generation if possible
            clearInterval(session.generationHandle);
        }
        this.streamingSessions.delete(streamId);
    },

    // Cleanup all resources
    cleanup: function() {
        // Stop all streaming sessions
        for (const [streamId, session] of this.streamingSessions) {
            if (session.generationHandle) {
                clearInterval(session.generationHandle);
            }
        }
        this.streamingSessions.clear();

        // Cleanup models
        this.activeModels.clear();
        
        console.log('LLamaSharp WASM backend cleaned up');
    }
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    console.log('LLamaSharp WASM JavaScript interface loaded');
});

// Cleanup on page unload
window.addEventListener('beforeunload', function() {
    if (window.llamaSharpWasm) {
        window.llamaSharpWasm.cleanup();
    }
});