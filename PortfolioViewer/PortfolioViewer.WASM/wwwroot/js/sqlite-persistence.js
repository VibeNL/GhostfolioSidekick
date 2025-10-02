// Type definitions for Blazor runtime and IndexedDB operations
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
export function waitForBlazorRuntime() {
    return new Promise((resolve) => {
        function checkRuntime() {
            var _a, _b, _c;
            if ((_c = (_b = (_a = window.Blazor) === null || _a === void 0 ? void 0 : _a.runtime) === null || _b === void 0 ? void 0 : _b.Module) === null || _c === void 0 ? void 0 : _c.FS) {
                console.log('Blazor runtime is ready');
                resolve();
            }
            else {
                console.log('Waiting for Blazor runtime...');
                setTimeout(checkRuntime, 100);
            }
        }
        checkRuntime();
    });
}
export function debugFileSystem(filename) {
    var _a, _b, _c;
    try {
        const path = `/${filename}`;
        console.log(`=== Database Debug Info for ${filename} ===`);
        // Check if Blazor runtime is available
        if (!((_c = (_b = (_a = window.Blazor) === null || _a === void 0 ? void 0 : _a.runtime) === null || _b === void 0 ? void 0 : _b.Module) === null || _c === void 0 ? void 0 : _c.FS)) {
            console.error('Blazor runtime or FS module not available');
            return;
        }
        // List root directory contents
        try {
            const rootContents = window.Blazor.runtime.Module.FS.readdir('/');
            console.log('Root directory contents:', rootContents);
        }
        catch (e) {
            console.error('Error reading root directory:', e);
        }
        // Check if the database file exists
        const pathInfo = window.Blazor.runtime.Module.FS.analyzePath(path);
        console.log(`Database file exists: ${pathInfo.exists}`);
        if (pathInfo.exists) {
            const stat = window.Blazor.runtime.Module.FS.stat(path);
            console.log(`Database file size: ${stat.size} bytes`);
        }
    }
    catch (error) {
        console.error('Error in debugFileSystem:', error);
    }
}
export function setupDatabase(filename) {
    return __awaiter(this, void 0, void 0, function* () {
        // Wait for Blazor runtime to be available
        yield waitForBlazorRuntime();
        return new Promise((resolve, reject) => {
            console.log(`Setting up database: ${filename}`);
            // Open (or create) the IndexedDB database that will store our SQLite file
            const dbRequest = window.indexedDB.open('SqliteStorage', 1);
            // This event fires if the database doesn't exist or needs version upgrade
            dbRequest.onupgradeneeded = (event) => {
                console.log('Database upgrade needed - creating object store');
                const target = event.target;
                const db = target.result;
                // Create the object store if it doesn't exist
                // This is where we'll store our SQLite database file as a binary blob
                if (!db.objectStoreNames.contains('Files')) {
                    db.createObjectStore('Files', { keyPath: 'id' });
                    console.log('Created Files object store');
                }
            };
            // Handle any errors opening IndexedDB
            dbRequest.onerror = () => {
                console.error('Error opening database:', dbRequest.error);
                reject(dbRequest.error);
            };
            // This fires after IndexedDB is successfully opened
            dbRequest.onsuccess = () => {
                console.log('Database opened successfully');
                const db = dbRequest.result;
                // Double-check that our object store exists
                if (!db.objectStoreNames.contains('Files')) {
                    console.error('Files object store not found');
                    reject(new Error('Files object store not found'));
                    return;
                }
                // Try to retrieve the SQLite database file from IndexedDB
                const getRequest = db.transaction('Files', 'readonly')
                    .objectStore('Files')
                    .get('database'); // We use 'database' as the key for our SQLite file
                getRequest.onsuccess = () => {
                    const path = `/${filename}`;
                    try {
                        const result = getRequest.result;
                        // Check if we found a database in IndexedDB
                        if (result) {
                            console.log('Found existing database in IndexedDB, size:', result.data.length, 'bytes');
                            // If database already exists in the virtual filesystem, remove it first
                            if (window.Blazor.runtime.Module.FS.analyzePath(path).exists) {
                                console.log("Database file already exists on local file system, removing it to create a new file from IndexedDB.");
                                window.Blazor.runtime.Module.FS.unlink(path);
                            }
                            // Create the database file in the virtual filesystem using the data from IndexedDB
                            // Parameters: directory, filename, data, canRead, canWrite, canOwn
                            window.Blazor.runtime.Module.FS.createDataFile('/', filename, result.data, true, true, true);
                            console.log("Database synced from IndexedDB to file system");
                            // Verify the file was created correctly
                            if (window.Blazor.runtime.Module.FS.analyzePath(path).exists) {
                                const fileSize = window.Blazor.runtime.Module.FS.stat(path).size;
                                console.log(`Verified: Database file created with size: ${fileSize} bytes`);
                            }
                            else {
                                console.error('Failed to create database file from IndexedDB data');
                            }
                        }
                        else {
                            // No database found in IndexedDB - this is normal for first run
                            console.log('No existing database found in IndexedDB');
                        }
                        // Debug the file system state
                        debugFileSystem(filename);
                        resolve();
                    }
                    catch (error) {
                        console.error('Error during file system operations:', error);
                        reject(error);
                    }
                };
                // Handle errors reading from IndexedDB
                getRequest.onerror = () => {
                    console.error('Error reading from database:', getRequest.error);
                    reject(getRequest.error);
                };
            };
        });
    });
}
export function clearDatabaseFromIndexedDb() {
    return __awaiter(this, void 0, void 0, function* () {
        return new Promise((resolve, reject) => {
            console.log('Clearing database from IndexedDB');
            // Open the IndexedDB database
            const dbRequest = window.indexedDB.open('SqliteStorage', 1);
            // Handle any errors opening IndexedDB
            dbRequest.onerror = () => {
                console.error('Error opening database for clearing:', dbRequest.error);
                reject(dbRequest.error);
            };
            // This fires after IndexedDB is successfully opened
            dbRequest.onsuccess = () => {
                const db = dbRequest.result;
                try {
                    // Verify the object store exists
                    if (!db.objectStoreNames.contains('Files')) {
                        console.log('Files object store not found, nothing to clear');
                        resolve();
                        return;
                    }
                    // Start a transaction and get the object store
                    const transaction = db.transaction('Files', 'readwrite');
                    const objectStore = transaction.objectStore('Files');
                    // Delete the database record from IndexedDB
                    const deleteRequest = objectStore.delete('database');
                    deleteRequest.onsuccess = () => {
                        console.log('Database successfully cleared from IndexedDB');
                        resolve();
                    };
                    deleteRequest.onerror = () => {
                        console.error('Error clearing database from IndexedDB:', deleteRequest.error);
                        reject(deleteRequest.error);
                    };
                }
                catch (error) {
                    console.error('Error during clear operation:', error);
                    reject(error);
                }
            };
        });
    });
}
export function syncDatabaseToIndexedDb(filename) {
    return __awaiter(this, void 0, void 0, function* () {
        // Wait for Blazor runtime to be available
        yield waitForBlazorRuntime();
        return new Promise((resolve, reject) => {
            console.log(`Syncing database to IndexedDB: ${filename}`);
            // Debug before sync
            debugFileSystem(filename);
            // Open the IndexedDB database
            const dbRequest = window.indexedDB.open('SqliteStorage', 1);
            // Handle any errors opening IndexedDB
            dbRequest.onerror = () => {
                console.error('Error opening database for sync:', dbRequest.error);
                reject(dbRequest.error);
            };
            // This fires after IndexedDB is successfully opened
            dbRequest.onsuccess = () => {
                const path = `/${filename}`;
                const db = dbRequest.result;
                try {
                    // Check if the database file exists in the virtual filesystem
                    if (window.Blazor.runtime.Module.FS.analyzePath(path).exists) {
                        // Read the entire database file as a binary blob
                        const data = window.Blazor.runtime.Module.FS.readFile(path);
                        console.log(`Reading database file, size: ${data.length} bytes`);
                        // Verify the object store exists
                        if (!db.objectStoreNames.contains('Files')) {
                            console.error('Files object store not found during sync');
                            reject(new Error('Files object store not found'));
                            return;
                        }
                        // Create an object to store in IndexedDB
                        // The 'id' property is the key, and 'data' contains the binary database
                        const dbObject = {
                            id: 'database', // Use a fixed key so we always update the same record
                            data: data // The binary database file content
                        };
                        // Start a transaction and get the object store
                        const transaction = db.transaction('Files', 'readwrite');
                        const objectStore = transaction.objectStore('Files');
                        // Store the database file in IndexedDB
                        // put() will add or update the record with the same key
                        const putRequest = objectStore.put(dbObject);
                        putRequest.onsuccess = () => {
                            console.log('Database successfully synced to IndexedDB');
                            resolve();
                        };
                        putRequest.onerror = () => {
                            console.error('Error syncing to IndexedDB:', putRequest.error);
                            reject(putRequest.error);
                        };
                    }
                    else {
                        console.log('Database file does not exist, nothing to sync');
                        resolve();
                    }
                }
                catch (error) {
                    console.error('Error during sync operation:', error);
                    reject(error);
                }
            };
        });
    });
}
