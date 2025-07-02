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

    // Check if model exists
    async hasModel(modelId) {
        try {
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

    // Get storage usage info
    async getStorageInfo() {
        try {
            if ('storage' in navigator && 'estimate' in navigator.storage) {
                const estimate = await navigator.storage.estimate();
                return {
                    quota: estimate.quota,
                    usage: estimate.usage,
                    available: estimate.quota - estimate.usage
                };
            }
            return null;
        } catch (error) {
            console.error('Error getting storage info:', error);
            return null;
        }
    }
};
