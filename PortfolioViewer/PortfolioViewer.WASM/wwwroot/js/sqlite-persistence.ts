// Type definitions for Blazor runtime and IndexedDB operations

declare global {
    interface Window {
        indexedDB: IDBFactory;
        Blazor: {
            runtime: {
                Module: {
                    FS: {
                        analyzePath(path: string): { exists: boolean };
                        unlink(path: string): void;
                        createDataFile(
                            parent: string,
                            name: string,
                            data: Uint8Array,
                            canRead: boolean,
                            canWrite: boolean,
                            canOwn: boolean
                        ): void;
                        readFile(path: string): Uint8Array;
                        stat(path: string): { size: number };
                    };
                };
            };
        };
    }
}

interface DatabaseRecord {
    id: string;
    data: Uint8Array;
}

interface IDBOpenDBRequestExtended extends IDBOpenDBRequest {
    result: IDBDatabase;
}

interface IDBRequestExtended<T = any> extends IDBRequest<T> {
    result: T;
}

export function setupDatabase(filename: string): Promise<void> {
    return new Promise<void>((resolve, reject) => {
        console.log(`Setting up database: ${filename}`);

        // Open (or create) the IndexedDB database that will store our SQLite file
        const dbRequest: IDBOpenDBRequest = window.indexedDB.open('SqliteStorage', 1);

        // This event fires if the database doesn't exist or needs version upgrade
        dbRequest.onupgradeneeded = (event: IDBVersionChangeEvent): void => {
            console.log('Database upgrade needed - creating object store');
            const target = event.target as IDBOpenDBRequestExtended;
            const db: IDBDatabase = target.result;

            // Create the object store if it doesn't exist
            // This is where we'll store our SQLite database file as a binary blob
            if (!db.objectStoreNames.contains('Files')) {
                db.createObjectStore('Files', { keyPath: 'id' });
                console.log('Created Files object store');
            }
        };

        // Handle any errors opening IndexedDB
        dbRequest.onerror = (): void => {
            console.error('Error opening database:', dbRequest.error);
            reject(dbRequest.error);
        };

        // This fires after IndexedDB is successfully opened
        dbRequest.onsuccess = (): void => {

            console.log('Database opened successfully');

            const db: IDBDatabase = (dbRequest as IDBOpenDBRequestExtended).result;

            // Double-check that our object store exists
            if (!db.objectStoreNames.contains('Files')) {
                console.error('Files object store not found');
                reject(new Error('Files object store not found'));
                return;
            }

            // Try to retrieve the SQLite database file from IndexedDB
            const getRequest: IDBRequest<DatabaseRecord | undefined> = db.transaction('Files', 'readonly')
                .objectStore('Files')
                .get('database'); // We use 'database' as the key for our SQLite file

            getRequest.onsuccess = (): void => {
                const path: string = `/${filename}`;

                try {
                    const result: DatabaseRecord | undefined = getRequest.result;

                    // Check if we found a database in IndexedDB
                    if (result) {
                        console.log('Found existing database in IndexedDB, size:', result.data.length, 'bytes');

                        // If database already exists in the virtual filesystem, remove it first
                        if (window.Blazor.runtime.Module.FS.analyzePath(path).exists) {
                            console.log("Database file already exists on local file system, removing it to create a new file from IndexedDB.")
                            window.Blazor.runtime.Module.FS.unlink(path);
                        }

                        // Create the database file in the virtual filesystem using the data from IndexedDB
                        // Parameters: directory, filename, data, canRead, canWrite, canOwn
                        window.Blazor.runtime.Module.FS.createDataFile('/', filename, result.data, true, true, true);
                        console.log("Database synced from IndexedDB to file system");

                        // Verify the file was created correctly
                        if (window.Blazor.runtime.Module.FS.analyzePath(path).exists) {
                            const fileSize: number = window.Blazor.runtime.Module.FS.stat(path).size;
                            console.log(`Verified: Database file created with size: ${fileSize} bytes`);
                        } else {
                            console.error('Failed to create database file from IndexedDB data');
                        }
                    } else {
                        // No database found in IndexedDB - this is normal for first run
                        console.log('No existing database found in IndexedDB');
                    }
                    resolve();
                } catch (error) {
                    console.error('Error during file system operations:', error);
                    reject(error);
                }
            };

            // Handle errors reading from IndexedDB
            getRequest.onerror = (): void => {
                console.error('Error reading from database:', getRequest.error);
                reject(getRequest.error);
            };
        };
    });
}

export function syncDatabaseToIndexedDb(filename: string): Promise<void> {
    return new Promise<void>((resolve, reject) => {
        console.log(`Syncing database to IndexedDB: ${filename}`);

        // Open the IndexedDB database
        const dbRequest: IDBOpenDBRequest = window.indexedDB.open('SqliteStorage', 1);

        // Handle any errors opening IndexedDB
        dbRequest.onerror = (): void => {
            console.error('Error opening database for sync:', dbRequest.error);
            reject(dbRequest.error);
        };

        // This fires after IndexedDB is successfully opened
        dbRequest.onsuccess = (): void => {
            const path: string = `/${filename}`;
            const db: IDBDatabase = (dbRequest as IDBOpenDBRequestExtended).result;

            try {
                // Check if the database file exists in the virtual filesystem
                if (window.Blazor.runtime.Module.FS.analyzePath(path).exists) {
                    // Read the entire database file as a binary blob
                    const data: Uint8Array = window.Blazor.runtime.Module.FS.readFile(path);
                    console.log(`Reading database file, size: ${data.length} bytes`);

                    // Verify the object store exists
                    if (!db.objectStoreNames.contains('Files')) {
                        console.error('Files object store not found during sync');
                        reject(new Error('Files object store not found'));
                        return;
                    }

                    // Create an object to store in IndexedDB
                    // The 'id' property is the key, and 'data' contains the binary database
                    const dbObject: DatabaseRecord = {
                        id: 'database', // Use a fixed key so we always update the same record
                        data: data      // The binary database file content
                    };

                    // Start a transaction and get the object store
                    const transaction: IDBTransaction = db.transaction('Files', 'readwrite');
                    const objectStore: IDBObjectStore = transaction.objectStore('Files');

                    // Store the database file in IndexedDB
                    // put() will add or update the record with the same key
                    const putRequest: IDBRequest<IDBValidKey> = objectStore.put(dbObject);

                    putRequest.onsuccess = (): void => {
                        console.log('Database successfully synced to IndexedDB');
                        resolve();
                    };

                    putRequest.onerror = (): void => {
                        console.error('Error syncing to IndexedDB:', putRequest.error);
                        reject(putRequest.error);
                    };
                } else {
                    console.log('Database file does not exist, nothing to sync');
                    resolve();
                }
            } catch (error) {
                console.error('Error during sync operation:', error);
                reject(error);
            }
        };
    });
}