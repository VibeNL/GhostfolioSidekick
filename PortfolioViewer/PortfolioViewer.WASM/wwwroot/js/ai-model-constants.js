// AI Model Constants for JavaScript/TypeScript
// This file centralizes all AI model configurations to match the C# AIModelConstants

window.AIModelConstants = {
    // Model definitions
    Models: {
        Phi3Mini4K: {
            id: 'phi3-mini-4k',
            name: 'Phi-3 Mini 4K Instruct',
            filename: 'Phi-3-mini-4k-instruct-q4.gguf',
            url: 'https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf',
            sizeBytes: 2240000000, // ~2.24GB
            browserStorageKey: 'llama_model_phi3_mini',
            virtualPathTemplate: '/models/{0}', // Use with format replacement
            
            // Alternative filenames that should map to this model
            alternativeFilenames: [
                'phi-3-mini-4k-instruct.q4_0.gguf',
                'phi-3-mini-4k-instruct.Q4_0.gguf',
                'phi-3-mini-4k-instruct-q4.gguf',
                'Phi-3-mini-4k-instruct-q4.gguf'
            ],
            
            // Partial name patterns that should match this model
            namePatterns: [
                'phi-3-mini',
                'phi3-mini',
                'Phi-3-mini',
                'PHI-3-MINI'
            ]
        }
    },

    // WebLLM models
    WebLLMModels: {
        defaultModelId: 'Qwen3-4B-q4f32_1-MLC'
    },

    // Storage and caching configuration
    Storage: {
        indexedDBName: 'AIModelsDB',
        indexedDBVersion: 1,
        defaultChunkSize: 100 * 1024 * 1024, // 100MB chunks
        maxRetries: 3,
        modelKeyPrefix: 'llama_model_'
    },

    // Gets the browser storage key for a given model filename
    getStorageKeyForModel: function(modelFilename) {
        if (!modelFilename) {
            throw new Error('Model filename cannot be null or empty');
        }

        const normalizedFilename = modelFilename.toLowerCase();

        // Check Phi-3 Mini 4K model
        if (this._isMatchingModel(
            normalizedFilename, 
            this.Models.Phi3Mini4K.filename, 
            this.Models.Phi3Mini4K.alternativeFilenames, 
            this.Models.Phi3Mini4K.namePatterns)) {
            return this.Models.Phi3Mini4K.browserStorageKey;
        }

        // For unknown models, use a sanitized version of the filename
        return this.Storage.modelKeyPrefix + this._sanitizeFilename(modelFilename);
    },

    // Gets the virtual path for a model filename in WASM environment
    getVirtualPath: function(modelFilename) {
        if (!modelFilename) {
            throw new Error('Model filename cannot be null or empty');
        }

        return this.Models.Phi3Mini4K.virtualPathTemplate.replace('{0}', modelFilename);
    },

    // Gets model configuration by filename
    getModelConfig: function(modelFilename) {
        if (!modelFilename) {
            return null;
        }

        const normalizedFilename = modelFilename.toLowerCase();

        // Check Phi-3 Mini 4K model
        if (this._isMatchingModel(
            normalizedFilename, 
            this.Models.Phi3Mini4K.filename, 
            this.Models.Phi3Mini4K.alternativeFilenames, 
            this.Models.Phi3Mini4K.namePatterns)) {
            return {
                id: this.Models.Phi3Mini4K.id,
                name: this.Models.Phi3Mini4K.name,
                filename: this.Models.Phi3Mini4K.filename,
                url: this.Models.Phi3Mini4K.url,
                sizeBytes: this.Models.Phi3Mini4K.sizeBytes,
                browserStorageKey: this.Models.Phi3Mini4K.browserStorageKey,
                virtualPath: this.getVirtualPath(this.Models.Phi3Mini4K.filename)
            };
        }

        return null;
    },

    // Helper method to check if a filename matches a model
    _isMatchingModel: function(normalizedFilename, primaryFilename, alternativeFilenames, namePatterns) {
        // Check exact match with primary filename
        if (normalizedFilename === primaryFilename.toLowerCase()) {
            return true;
        }

        // Check alternative filenames
        for (const altFilename of alternativeFilenames) {
            if (normalizedFilename === altFilename.toLowerCase()) {
                return true;
            }
        }

        // Check name patterns
        for (const pattern of namePatterns) {
            if (normalizedFilename.includes(pattern.toLowerCase())) {
                return true;
            }
        }

        return false;
    },

    // Sanitizes a filename for use as a storage key
    _sanitizeFilename: function(filename) {
        return filename.replace(/\./g, '_').replace(/-/g, '_').toLowerCase();
    }
};