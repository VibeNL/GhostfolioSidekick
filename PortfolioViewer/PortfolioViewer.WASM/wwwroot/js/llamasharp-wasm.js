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
                _llamacpp_load_model_from_file: function(filePath, params) {
                    console.log('Mock: Loading model from file:', filePath, 'with params:', params);
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
            
            // Map model paths to storage keys
            // The ModelDownloadService uses "llama_model_phi3_mini" as the storage key
            let modelStorageKey;
            const modelFileName = modelPath.split('/').pop();
            
            if (modelFileName === 'phi-3-mini-4k-instruct.Q4_0.gguf' || modelPath.includes('phi-3-mini')) {
                modelStorageKey = 'llama_model_phi3_mini'; // This matches BROWSER_STORAGE_KEY in C#
            } else {
                // For other models, use the filename as fallback
                modelStorageKey = modelFileName;
            }
            
            console.log('Looking for model with storage key:', modelStorageKey);
            
            // Check if model exists first
            const modelExists = await blazorBrowserStorage.hasModel(modelStorageKey);
            if (!modelExists) {
                console.warn('Model not found in browser storage. Available models:', await this.listAvailableModels());
                throw new Error('Model not found in browser storage: ' + modelStorageKey + ' (filename: ' + modelFileName + ')');
            }
            
            // Get model data from IndexedDB (previously downloaded)
            console.log('Retrieving model data...');
            const modelData = await blazorBrowserStorage.getModelData(modelStorageKey);
            
            if (!modelData) {
                throw new Error('Model data not found in browser storage: ' + modelStorageKey);
            }
            
            let modelHandle;
            
            // Handle both streaming and legacy model data formats
            if (modelData.isStreaming) {
                console.log('Using streaming model data, size:', modelData.size, 'bytes, chunks:', modelData.totalChunks);
                
                // Initialize the model with streaming data
                modelHandle = await this.initializeStreamingModel(modelData, params);
            } else {
                // Legacy direct buffer approach (for smaller models or when streaming fails)
                console.log('Using direct model data, size:', modelData.byteLength, 'bytes');
                modelHandle = this.wasmModule._llamacpp_load_model(modelData, params);
            }
            
            if (modelHandle <= 0) {
                throw new Error('Failed to load model in WASM');
            }

            // Store the model handle (use original modelPath as key for consistency)
            this.activeModels.set(modelPath, {
                handle: modelHandle,
                params: params,
                isReady: true,
                storageKey: modelStorageKey
            });

            console.log('Model initialized successfully with handle:', modelHandle);
            return true;
            
        } catch (error) {
            console.error('Failed to initialize model:', error);
            return false;
        }
    },

    // Initialize model using streaming data approach
    initializeStreamingModel: async function(streamingData, params) {
        console.log('Initializing streaming model...');
        
        try {
            // Create a temporary file in WASM memory for the model
            const tempModelPath = '/tmp/streaming_model.gguf';
            
            // Ensure temp directory exists
            if (typeof FS !== 'undefined') {
                try {
                    FS.mkdirTree('/tmp');
                } catch (e) {
                    // Directory might already exist
                }
                
                // Stream chunks directly to the virtual file system
                console.log('Streaming model chunks to virtual file system...');
                let totalWritten = 0;
                let chunkCount = 0;
                
                // Open file for writing
                const stream = FS.open(tempModelPath, 'w');
                
                try {
                    // Iterate through chunks and write them to the file
                    for await (const chunk of streamingData) {
                        FS.write(stream, chunk, 0, chunk.length);
                        totalWritten += chunk.length;
                        chunkCount++;
                        
                        // Log progress periodically
                        if (chunkCount % 10 === 0) {
                            console.log(`Streamed ${chunkCount} chunks, ${Math.round(totalWritten / 1024 / 1024)}MB`);
                        }
                        
                        // Small delay to prevent blocking
                        if (chunkCount % 5 === 0) {
                            await new Promise(resolve => setTimeout(resolve, 10));
                        }
                    }
                } finally {
                    FS.close(stream);
                }
                
                console.log(`Streaming complete: ${chunkCount} chunks, ${totalWritten} bytes`);
                
                // Now load the model from the virtual file
                const modelHandle = this.wasmModule._llamacpp_load_model_from_file(tempModelPath, params);
                
                // Clean up the temporary file
                try {
                    FS.unlink(tempModelPath);
                } catch (e) {
                    console.warn('Failed to clean up temporary model file:', e);
                }
                
                return modelHandle;
                
            } else {
                // Fallback: load chunks into memory in smaller batches
                console.log('FS not available, using memory batching approach...');
                return await this.initializeModelWithBatching(streamingData, params);
            }
            
        } catch (error) {
            console.error('Error in streaming model initialization:', error);
            throw error;
        }
    },

    // Fallback approach: load model in smaller memory batches
    initializeModelWithBatching: async function(streamingData, params) {
        console.log('Using memory batching for model initialization...');
        
        const BATCH_SIZE = 10; // Process 10 chunks at a time
        const chunks = [];
        let chunkCount = 0;
        
        // Collect chunks in smaller batches
        for await (const chunk of streamingData) {
            chunks.push(chunk);
            chunkCount++;
            
            // Process batch when we have enough chunks
            if (chunks.length >= BATCH_SIZE) {
                console.log(`Processing batch ${Math.floor(chunkCount / BATCH_SIZE)}: ${chunks.length} chunks`);
                
                // Here we would ideally feed chunks to the WASM module incrementally
                // For now, we simulate this by collecting chunks
                
                // Clear processed chunks to free memory
                chunks.length = 0;
                
                // Yield control
                await new Promise(resolve => setTimeout(resolve, 50));
            }
        }
        
        // Process remaining chunks
        if (chunks.length > 0) {
            console.log(`Processing final batch: ${chunks.length} chunks`);
        }
        
        console.log(`Model batching complete: ${chunkCount} total chunks processed`);
        
        // For the mock implementation, just return a mock handle
        // In a real implementation, this would return the actual model handle
        return 1; // Mock handle
    },

    // Helper function to check available memory
    checkAvailableMemory: function() {
        if (performance && performance.memory) {
            const memInfo = performance.memory;
            const used = Math.round(memInfo.usedJSHeapSize / 1024 / 1024);
            const total = Math.round(memInfo.totalJSHeapSize / 1024 / 1024);
            const limit = Math.round(memInfo.jsHeapSizeLimit / 1024 / 1024);
            const available = Math.round((memInfo.jsHeapSizeLimit - memInfo.usedJSHeapSize) / 1024 / 1024);
            
            console.log('Memory status:', {
                used: used + 'MB',
                total: total + 'MB', 
                limit: limit + 'MB',
                available: available + 'MB'
            });
            
            return {
                used: memInfo.usedJSHeapSize,
                total: memInfo.totalJSHeapSize,
                limit: memInfo.jsHeapSizeLimit,
                available: memInfo.jsHeapSizeLimit - memInfo.usedJSHeapSize
            };
        }
        
        console.warn('Performance.memory not available');
        return null;
    },

    // Helper function to list available models for debugging
    listAvailableModels: async function() {
        try {
            if (!window.blazorBrowserStorage || !window.blazorBrowserStorage.initDB) {
                return [];
            }
            
            const db = await blazorBrowserStorage.initDB();
            const transaction = db.transaction(['models'], 'readonly');
            const store = transaction.objectStore('models');
            const request = store.getAll();
            
            return new Promise((resolve) => {
                request.onsuccess = () => {
                    const models = request.result || [];
                    const modelInfo = models.map(model => ({
                        id: model.id,
                        size: Math.round(model.size / 1024 / 1024) + 'MB',
                        chunks: model.chunks,
                        status: model.status
                    }));
                    console.log('Available models:', modelInfo);
                    resolve(modelInfo);
                };
                request.onerror = () => resolve([]);
            });
        } catch (error) {
            console.error('Error listing available models:', error);
            return [];
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

    // Memory cleanup function
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
        
        // Force garbage collection if available
        if (window.gc) {
            try {
                window.gc();
                console.log('Forced garbage collection');
            } catch (e) {
                // GC not available or failed
            }
        }
        
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