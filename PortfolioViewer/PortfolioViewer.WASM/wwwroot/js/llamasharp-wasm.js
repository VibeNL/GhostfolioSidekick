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
            
            // Get model data from IndexedDB (previously downloaded)
            const modelData = await blazorBrowserStorage.getModelData(modelPath.split('/').pop());
            
            if (!modelData) {
                throw new Error('Model data not found in browser storage');
            }

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

// Browser storage interface for model files
window.blazorBrowserStorage = window.blazorBrowserStorage || {
    // Mock implementation - in a real scenario, this would interface with IndexedDB
    storage: new Map(),
    
    hasModel: function(key) {
        try {
            if (!key) return false;
            return this.storage.has(key);
        } catch (error) {
            console.error('Error checking if model exists:', error);
            return false;
        }
    },
    
    getModelSize: function(key) {
        try {
            if (!key) return 0;
            const data = this.storage.get(key);
            return data ? data.length : 0;
        } catch (error) {
            console.error('Error getting model size:', error);
            return 0;
        }
    },
    
    getModelData: function(key) {
        try {
            if (!key) return null;
            return this.storage.get(key);
        } catch (error) {
            console.error('Error getting model data:', error);
            return null;
        }
    },
    
    initializeModelDownload: function(key, totalSize) {
        try {
            if (!key || totalSize <= 0) {
                throw new Error('Invalid parameters for model download initialization');
            }
            this.storage.set(key + '_chunks', []);
            this.storage.set(key + '_totalSize', totalSize);
            console.log('Initialized download for:', key, 'size:', totalSize);
            return true;
        } catch (error) {
            console.error('Error initializing model download:', error);
            return false;
        }
    },
    
    appendModelChunk: function(key, chunkData) {
        try {
            if (!key || !chunkData) {
                throw new Error('Invalid parameters for chunk append');
            }
            const chunks = this.storage.get(key + '_chunks') || [];
            chunks.push(chunkData);
            this.storage.set(key + '_chunks', chunks);
            console.log('Appended chunk for:', key, 'chunk size:', chunkData.length);
            return true;
        } catch (error) {
            console.error('Error appending model chunk:', error);
            return false;
        }
    },
    
    finalizeModelDownload: function(key) {
        try {
            if (!key) {
                throw new Error('Invalid key for model finalization');
            }
            
            const chunks = this.storage.get(key + '_chunks') || [];
            if (chunks.length === 0) {
                throw new Error('No chunks found for model: ' + key);
            }
            
            // Combine all chunks into a single ArrayBuffer
            const totalSize = chunks.reduce((sum, chunk) => sum + chunk.length, 0);
            const combined = new Uint8Array(totalSize);
            
            let offset = 0;
            for (const chunk of chunks) {
                combined.set(new Uint8Array(chunk), offset);
                offset += chunk.length;
            }
            
            // Store the complete model
            this.storage.set(key, combined.buffer);
            
            // Cleanup chunks
            this.storage.delete(key + '_chunks');
            this.storage.delete(key + '_totalSize');
            
            console.log('Finalized model download for:', key, 'final size:', combined.length);
            return true;
            
        } catch (error) {
            console.error('Error finalizing model download:', error);
            return false;
        }
    },
    
    mountModel: function(key, virtualPath) {
        try {
            if (!key || !virtualPath) {
                throw new Error('Invalid parameters for model mounting');
            }
            // In a real implementation, this would mount the model file to Emscripten's virtual file system
            console.log('Mounted model:', key, 'to virtual path:', virtualPath);
            return true;
        } catch (error) {
            console.error('Error mounting model:', error);
            return false;
        }
    },
    
    deleteModel: function(key) {
        try {
            if (!key) return true; // Nothing to delete
            
            this.storage.delete(key);
            this.storage.delete(key + '_chunks');
            this.storage.delete(key + '_totalSize');
            console.log('Deleted model:', key);
            return true;
        } catch (error) {
            console.error('Error deleting model:', error);
            return false;
        }
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