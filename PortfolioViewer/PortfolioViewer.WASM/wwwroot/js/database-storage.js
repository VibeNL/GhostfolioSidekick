"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
class DatabaseStorage {
    constructor(options) {
        this.dbName = options.dbName;
        this.storeName = options.storeName;
        this.version = options.version;
    }
    openDB() {
        return __awaiter(this, void 0, void 0, function* () {
            return new Promise((resolve, reject) => {
                const request = indexedDB.open(this.dbName, this.version);
                request.onerror = () => reject(request.error);
                request.onsuccess = () => resolve(request.result);
                request.onupgradeneeded = (event) => {
                    const db = event.target.result;
                    if (!db.objectStoreNames.contains(this.storeName)) {
                        db.createObjectStore(this.storeName);
                    }
                };
            });
        });
    }
    saveDatabase(databaseBuffer) {
        return __awaiter(this, void 0, void 0, function* () {
            const db = yield this.openDB();
            const transaction = db.transaction([this.storeName], 'readwrite');
            const store = transaction.objectStore(this.storeName);
            return new Promise((resolve, reject) => {
                const request = store.put(databaseBuffer, 'database');
                request.onerror = () => reject(request.error);
                request.onsuccess = () => resolve();
            });
        });
    }
    loadDatabase() {
        return __awaiter(this, void 0, void 0, function* () {
            const db = yield this.openDB();
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
        });
    }
    clearDatabase() {
        return __awaiter(this, void 0, void 0, function* () {
            const db = yield this.openDB();
            const transaction = db.transaction([this.storeName], 'readwrite');
            const store = transaction.objectStore(this.storeName);
            return new Promise((resolve, reject) => {
                const request = store.clear();
                request.onerror = () => reject(request.error);
                request.onsuccess = () => resolve();
            });
        });
    }
    databaseExists() {
        return __awaiter(this, void 0, void 0, function* () {
            try {
                const data = yield this.loadDatabase();
                return data !== null;
            }
            catch (_a) {
                return false;
            }
        });
    }
}
// Global instance for the portfolio database
const portfolioDatabaseStorage = new DatabaseStorage({
    dbName: 'PortfolioDatabase',
    storeName: 'sqlite_data',
    version: 1
});
// Export functions for .NET interop
window.DatabaseStorage = {
    saveDatabase: (databaseBuffer) => __awaiter(void 0, void 0, void 0, function* () {
        yield portfolioDatabaseStorage.saveDatabase(databaseBuffer);
    }),
    loadDatabase: () => __awaiter(void 0, void 0, void 0, function* () {
        return yield portfolioDatabaseStorage.loadDatabase();
    }),
    clearDatabase: () => __awaiter(void 0, void 0, void 0, function* () {
        yield portfolioDatabaseStorage.clearDatabase();
    }),
    databaseExists: () => __awaiter(void 0, void 0, void 0, function* () {
        return yield portfolioDatabaseStorage.databaseExists();
    })
};
