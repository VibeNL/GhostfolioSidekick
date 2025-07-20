interface IDBDatabaseWithName extends IDBDatabase {
    readonly name: string;
}

interface DatabaseStorageOptions {
    dbName: string;
    storeName: string;
    version: number;
}

class DatabaseStorage {
    private dbName: string;
    private storeName: string;
    private version: number;

    constructor(options: DatabaseStorageOptions) {
        this.dbName = options.dbName;
        this.storeName = options.storeName;
        this.version = options.version;
    }

    private async openDB(): Promise<IDBDatabase> {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(this.dbName, this.version);

            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve(request.result);

            request.onupgradeneeded = (event) => {
                const db = (event.target as IDBOpenDBRequest).result;
                if (!db.objectStoreNames.contains(this.storeName)) {
                    db.createObjectStore(this.storeName);
                }
            };
        });
    }

    async saveDatabase(databaseBuffer: ArrayBuffer): Promise<void> {
        const db = await this.openDB();
        const transaction = db.transaction([this.storeName], 'readwrite');
        const store = transaction.objectStore(this.storeName);

        return new Promise((resolve, reject) => {
            const request = store.put(databaseBuffer, 'database');
            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve();
        });
    }

    async loadDatabase(): Promise<ArrayBuffer | null> {
        const db = await this.openDB();
        const transaction = db.transaction([this.storeName], 'readonly');
        const store = transaction.objectStore(this.storeName);

        return new Promise((resolve, reject) => {
            const request = store.get('database');
            request.onerror = () => reject(request.error);
            request.onsuccess = () => {
                const result = request.result;
                resolve(result || null);
            };
        });
    }

    async clearDatabase(): Promise<void> {
        const db = await this.openDB();
        const transaction = db.transaction([this.storeName], 'readwrite');
        const store = transaction.objectStore(this.storeName);

        return new Promise((resolve, reject) => {
            const request = store.clear();
            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve();
        });
    }

    async databaseExists(): Promise<boolean> {
        try {
            const data = await this.loadDatabase();
            return data !== null;
        } catch {
            return false;
        }
    }
}

// Global instance for the portfolio database
const portfolioDatabaseStorage = new DatabaseStorage({
    dbName: 'PortfolioDatabase',
    storeName: 'sqlite_data',
    version: 1
});

// Export functions for .NET interop
(window as any).DatabaseStorage = {
    saveDatabase: async (databaseBuffer: ArrayBuffer) => {
        await portfolioDatabaseStorage.saveDatabase(databaseBuffer);
    },
    
    loadDatabase: async (): Promise<ArrayBuffer | null> => {
        return await portfolioDatabaseStorage.loadDatabase();
    },
    
    clearDatabase: async () => {
        await portfolioDatabaseStorage.clearDatabase();
    },
    
    databaseExists: async (): Promise<boolean> => {
        return await portfolioDatabaseStorage.databaseExists();
    }
};