"use strict";

function scrollToBottom(id) {
    const element = document.getElementById(id);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}

// Browser storage for AI models using IndexedDB
window.blazorBrowserStorage = {
    dbName: 'AIModelsDB',
    dbVersion: 1,
    db: null,
    _initialized: false,

    // Initialize the storage system - call this before any other operations
    async initialize() {
        if (this._initialized) return true;
        
        try {
            await this.initDB();
            this._initialized = true;
            console.log('blazorBrowserStorage initialized successfully');
            return true;
        } catch (error) {
            console.error('Failed to initialize blazorBrowserStorage:', error);
            return false;
        }
    },

    // Initialize IndexedDB
    async initDB() {
        if (this.db) return this.db;

        return new Promise((resolve, reject) => {
            const request = indexedDB.open(this.dbName, this.dbVersion);
            
            request.onerror = () => reject(request.error);
            request.onsuccess = () => {
                this.db = request.result;
                resolve(this.db);
            };
            
            request.onupgradeneeded = (event) => {
                const db = event.target.result;
                
                // Create object store for models
                if (!db.objectStoreNames.contains('models')) {
                    const modelStore = db.createObjectStore('models', { keyPath: 'id' });
                    modelStore.createIndex('id', 'id', { unique: true });
                }
                
                // Create object store for model chunks (for streaming)
                if (!db.objectStoreNames.contains('chunks')) {
                    const chunkStore = db.createObjectStore('chunks', { keyPath: ['modelId', 'chunkIndex'] });
                    chunkStore.createIndex('modelId', 'modelId', { unique: false });
                }
            };
        });
    },

    // Check if storage is ready for use
    isReady() {
        return this._initialized && this.db !== null;
    },

    // Check if model exists
    async hasModel(modelId) {
        try {
            if (!this.isReady()) {
                await this.initialize();
            }
            
            const db = await this.initDB();
            const transaction = db.transaction(['models'], 'readonly');
            const store = transaction.objectStore('models');
            const request = store.get(modelId);
            
            return new Promise((resolve) => {
                request.onsuccess = () => resolve(!!request.result);
                request.onerror = () => resolve(false);
            });
        } catch (error) {
            console.error('Error checking model existence:', error);
            return false;
        }
    },

    // Get model size
    async getModelSize(modelId) {
        try {
            if (!this.isReady()) {
                await this.initialize();
            }
            
            const db = await this.initDB();
            const transaction = db.transaction(['models'], 'readonly');
            const store = transaction.objectStore('models');
            const request = store.get(modelId);
            
            return new Promise((resolve) => {
                request.onsuccess = () => {
                    const result = request.result;
                    resolve(result ? result.size : 0);
                };
                request.onerror = () => resolve(0);
            });
        } catch (error) {
            console.error('Error getting model size:', error);
            return 0;
        }
    },

    // Initialize model download
    async initializeModelDownload(modelId, expectedSize) {
        try {
            if (!this.isReady()) {
                const initialized = await this.initialize();
                if (!initialized) {
                    throw new Error('Failed to initialize browser storage system');
                }
            }
            
            const db = await this.initDB();
            
            // Clear any existing chunks for this model
            await this.deleteModel(modelId);
            
            // Create model entry
            const transaction = db.transaction(['models'], 'readwrite');
            const store = transaction.objectStore('models');
            
            const modelData = {
                id: modelId,
                size: 0,
                expectedSize: expectedSize,
                chunks: 0,
                status: 'downloading',
                created: new Date().toISOString()
            };
            
            store.put(modelData);
            return true;
        } catch (error) {
            console.error('Error initializing model download:', error);
            return false;
        }
    },

    // Append model chunk
    async appendModelChunk(modelId, chunkData) {
        try {
            if (!this.isReady()) {
                await this.initialize();
            }
            
            const db = await this.initDB();
            
            // Get current model info
            const modelTransaction = db.transaction(['models'], 'readonly');
            const modelStore = modelTransaction.objectStore('models');
            const modelRequest = modelStore.get(modelId);
            
            return new Promise((resolve, reject) => {
                modelRequest.onsuccess = () => {
                    const modelInfo = modelRequest.result;
                    if (!modelInfo) {
                        reject(new Error('Model not found'));
                        return;
                    }
                    
                    // Store chunk
                    const chunkTransaction = db.transaction(['chunks', 'models'], 'readwrite');
                    const chunkStore = chunkTransaction.objectStore('chunks');
                    const updateModelStore = chunkTransaction.objectStore('models');
                    
                    const chunkEntry = {
                        modelId: modelId,
                        chunkIndex: modelInfo.chunks,
                        data: chunkData,
                        size: chunkData.length
                    };
                    
                    chunkStore.add(chunkEntry);
                    
                    // Update model info
                    modelInfo.chunks += 1;
                    modelInfo.size += chunkData.length;
                    updateModelStore.put(modelInfo);
                    
                    chunkTransaction.oncomplete = () => resolve(true);
                    chunkTransaction.onerror = () => reject(chunkTransaction.error);
                };
                
                modelRequest.onerror = () => reject(modelRequest.error);
            });
        } catch (error) {
            console.error('Error appending model chunk:', error);
            throw error;
        }
    },

    // Finalize model download
    async finalizeModelDownload(modelId) {
        try {
            if (!this.isReady()) {
                await this.initialize();
            }
            
            const db = await this.initDB();
            const transaction = db.transaction(['models'], 'readwrite');
            const store = transaction.objectStore('models');
            const request = store.get(modelId);
            
            return new Promise((resolve, reject) => {
                request.onsuccess = () => {
                    const modelInfo = request.result;
                    if (modelInfo) {
                        modelInfo.status = 'complete';
                        modelInfo.completed = new Date().toISOString();
                        store.put(modelInfo);
                    }
                    resolve(true);
                };
                request.onerror = () => reject(request.error);
            });
        } catch (error) {
            console.error('Error finalizing model download:', error);
            return false;
        }
    },

    // Mount model to virtual file system (for WASM)
    async mountModel(modelId, virtualPath) {
        try {
            if (!this.isReady()) {
                await this.initialize();
            }
            
            // Create the virtual directory if it doesn't exist
            const dirPath = virtualPath.substring(0, virtualPath.lastIndexOf('/'));
            if (dirPath && typeof FS !== 'undefined') {
                try {
                    FS.mkdirTree(dirPath);
                } catch (e) {
                    // Directory might already exist
                }
            }
            
            // Get all chunks for this model
            const chunks = await this.getModelChunks(modelId);
            if (chunks.length === 0) {
                throw new Error('No model chunks found');
            }
            
            // Combine all chunks into a single Uint8Array
            let totalSize = 0;
            chunks.forEach(chunk => totalSize += chunk.data.length);
            
            const combinedData = new Uint8Array(totalSize);
            let offset = 0;
            
            // Sort chunks by index and combine
            chunks.sort((a, b) => a.chunkIndex - b.chunkIndex);
            for (const chunk of chunks) {
                combinedData.set(new Uint8Array(chunk.data), offset);
                offset += chunk.data.length;
            }
            
            // Write to virtual file system
            if (typeof FS !== 'undefined') {
                FS.writeFile(virtualPath, combinedData);
                console.log(`Model mounted at ${virtualPath}, size: ${combinedData.length} bytes`);
                return true;
            } else {
                console.warn('Emscripten FS not available, model stored in memory only');
                // Store in a global variable as fallback
                window.__wasmModelData = window.__wasmModelData || {};
                window.__wasmModelData[virtualPath] = combinedData;
                return true;
            }
        } catch (error) {
            console.error('Error mounting model:', error);
            return false;
        }
    },

    // Get all chunks for a model
    async getModelChunks(modelId) {
        try {
            if (!this.isReady()) {
                await this.initialize();
            }
            
            const db = await this.initDB();
            const transaction = db.transaction(['chunks'], 'readonly');
            const store = transaction.objectStore('chunks');
            const index = store.index('modelId');
            const request = index.getAll(modelId);
            
            return new Promise((resolve, reject) => {
                request.onsuccess = () => resolve(request.result || []);
                request.onerror = () => reject(request.error);
            });
        } catch (error) {
            console.error('Error getting model chunks:', error);
            return [];
        }
    },

    // Delete model and all its chunks
    async deleteModel(modelId) {
        try {
            if (!this.isReady()) {
                await this.initialize();
            }
            
            const db = await this.initDB();
            
            // Delete all chunks
            const chunkTransaction = db.transaction(['chunks'], 'readwrite');
            const chunkStore = chunkTransaction.objectStore('chunks');
            const chunkIndex = chunkStore.index('modelId');
            const chunkRequest = chunkIndex.getAllKeys(modelId);
            
            chunkRequest.onsuccess = () => {
                const keys = chunkRequest.result;
                keys.forEach(key => {
                    chunkStore.delete(key);
                });
            };
            
            // Delete model entry
            const modelTransaction = db.transaction(['models'], 'readwrite');
            const modelStore = modelTransaction.objectStore('models');
            modelStore.delete(modelId);
            
            return true;
        } catch (error) {
            console.error('Error deleting model:', error);
            return false;
        }
    },

    // Get model data (reconstructed from chunks) - STREAMING VERSION
    async getModelData(modelId) {
        try {
            if (!this.isReady()) {
                await this.initialize();
            }
            
            // Check if model exists and is complete
            const db = await this.initDB();
            const transaction = db.transaction(['models'], 'readonly');
            const store = transaction.objectStore('models');
            const request = store.get(modelId);
            
            const modelInfo = await new Promise((resolve, reject) => {
                request.onsuccess = () => resolve(request.result);
                request.onerror = () => reject(request.error);
            });
            
            if (!modelInfo || modelInfo.status !== 'complete') {
                console.warn('Model not found or not complete:', modelId);
                return null;
            }
            
            console.log('Model found in storage:', modelInfo);
            
            // Instead of loading all chunks into memory, return a streaming interface
            // This avoids the 2.4GB memory allocation issue
            return {
                size: modelInfo.size,
                totalChunks: modelInfo.chunks,
                isStreaming: true,
                modelId: modelId,
                
                // Method to get chunks on demand
                getChunk: async (chunkIndex) => {
                    const chunkTransaction = db.transaction(['chunks'], 'readonly');
                    const chunkStore = chunkTransaction.objectStore('chunks');
                    const chunkRequest = chunkStore.get([modelId, chunkIndex]);
                    
                    return new Promise((resolve, reject) => {
                        chunkRequest.onsuccess = () => {
                            const chunk = chunkRequest.result;
                            resolve(chunk ? chunk.data : null);
                        };
                        chunkRequest.onerror = () => reject(chunkRequest.error);
                    });
                },
                
                // Iterator for streaming chunks
                [Symbol.asyncIterator]: async function* () {
                    for (let i = 0; i < modelInfo.chunks; i++) {
                        const chunkData = await this.getChunk(i);
                        if (chunkData) {
                            yield new Uint8Array(chunkData);
                        }
                    }
                }
            };
            
        } catch (error) {
            console.error('Error getting model data:', error);
            return null;
        }
    },

    // Alternative method that tries smaller memory allocation with fallback
    async getModelDataLegacy(modelId) {
        try {
            if (!this.isReady()) {
                await this.initialize();
            }
            
            // Check if model exists and is complete
            const db = await this.initDB();
            const transaction = db.transaction(['models'], 'readonly');
            const store = transaction.objectStore('models');
            const request = store.get(modelId);
            
            const modelInfo = await new Promise((resolve, reject) => {
                request.onsuccess = () => resolve(request.result);
                request.onerror = () => reject(request.error);
            });
            
            if (!modelInfo || modelInfo.status !== 'complete') {
                console.warn('Model not found or not complete:', modelId);
                return null;
            }
            
            // Get all chunks for this model
            const chunks = await this.getModelChunks(modelId);
            if (chunks.length === 0) {
                console.warn('No chunks found for model:', modelId);
                return null;
            }
            
            console.log(`Attempting to reconstruct model from ${chunks.length} chunks`);
            
            // Try to use smaller memory footprint approach
            // Sort chunks by index first
            chunks.sort((a, b) => a.chunkIndex - b.chunkIndex);
            
            // Calculate total size
            let totalSize = 0;
            chunks.forEach(chunk => totalSize += chunk.data.byteLength);
            
            console.log(`Total model size: ${totalSize} bytes (${Math.round(totalSize / 1024 / 1024)}MB)`);
            
            // Check available memory before attempting allocation
            if (performance && performance.memory) {
                const memInfo = performance.memory;
                console.log('Memory info:', {
                    used: Math.round(memInfo.usedJSHeapSize / 1024 / 1024) + 'MB',
                    total: Math.round(memInfo.totalJSHeapSize / 1024 / 1024) + 'MB',
                    limit: Math.round(memInfo.jsHeapSizeLimit / 1024 / 1024) + 'MB'
                });
                
                // If we don't have enough memory, return streaming interface
                const availableMemory = memInfo.jsHeapSizeLimit - memInfo.usedJSHeapSize;
                if (totalSize > availableMemory * 0.5) { // Use only 50% of available memory
                    console.warn('Insufficient memory for full model loading, using streaming interface');
                    return await this.getModelData(modelId); // Use streaming version
                }
            }
            
            try {
                // Attempt to create the combined buffer
                const combinedData = new Uint8Array(totalSize);
                let offset = 0;
                
                // Combine chunks into single buffer
                for (const chunk of chunks) {
                    const chunkArray = new Uint8Array(chunk.data);
                    combinedData.set(chunkArray, offset);
                    offset += chunkArray.length;
                    
                    // Yield control periodically to prevent blocking
                    if (offset % (100 * 1024 * 1024) === 0) { // Every 100MB
                        await new Promise(resolve => setTimeout(resolve, 10));
                    }
                }
                
                console.log(`Successfully reconstructed model: ${combinedData.length} bytes`);
                return combinedData.buffer;
                
            } catch (allocError) {
                console.warn('Failed to allocate large buffer, falling back to streaming:', allocError.message);
                return await this.getModelData(modelId); // Use streaming version
            }
            
        } catch (error) {
            console.error('Error getting model data:', error);
            return null;
        }
    },
};

// Auto-initialize when the page loads
document.addEventListener('DOMContentLoaded', async function() {
    console.log('DOM loaded, initializing blazorBrowserStorage...');
    try {
        const initialized = await window.blazorBrowserStorage.initialize();
        if (initialized) {
            console.log('blazorBrowserStorage successfully initialized on DOMContentLoaded');
        } else {
            console.warn('blazorBrowserStorage failed to initialize on DOMContentLoaded');
        }
    } catch (error) {
        console.error('Failed to auto-initialize blazorBrowserStorage:', error);
    }
});

// Fallback initialization when Blazor is ready
window.addEventListener('load', async function() {
    console.log('Window load event, checking blazorBrowserStorage...');
    if (window.blazorBrowserStorage && !window.blazorBrowserStorage.isReady()) {
        try {
            const initialized = await window.blazorBrowserStorage.initialize();
            if (initialized) {
                console.log('blazorBrowserStorage successfully initialized on window load');
            } else {
                console.warn('blazorBrowserStorage failed to initialize on window load');
            }
        } catch (error) {
            console.error('Failed to initialize blazorBrowserStorage on window load:', error);
        }
    }
});

// Cleanup when page unloads
window.addEventListener('beforeunload', function() {
    if (window.blazorBrowserStorage && window.blazorBrowserStorage.db) {
        try {
            window.blazorBrowserStorage.db.close();
            console.log('Closed IndexedDB connection on page unload');
        } catch (error) {
            console.warn('Error closing IndexedDB:', error);
        }
    }
});

// Expose a function for Blazor to check if storage is ready
window.checkBrowserStorageReady = function() {
    return window.blazorBrowserStorage && window.blazorBrowserStorage.isReady();
};

// Expose a function for Blazor to manually initialize storage if needed
window.initializeBrowserStorage = async function() {
    if (window.blazorBrowserStorage) {
        return await window.blazorBrowserStorage.initialize();
    }
    console.error('blazorBrowserStorage object not found');
    return false;
};

// Debugging function to check storage status
window.debugBrowserStorage = function() {
    console.log('Browser Storage Debug Info:', {
        blazorBrowserStorageExists: !!window.blazorBrowserStorage,
        isInitialized: window.blazorBrowserStorage ? window.blazorBrowserStorage._initialized : false,
        isReady: window.blazorBrowserStorage ? window.blazorBrowserStorage.isReady() : false,
        dbExists: window.blazorBrowserStorage ? !!window.blazorBrowserStorage.db : false
    });
};
