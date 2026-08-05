const DB_NAME = 'd12canvas-app';
const STORE_NAME = 'boards';
const DB_VERSION = 1;

let dbPromise = null;

function openDatabase() {
    if (!dbPromise) {
        dbPromise = new Promise((resolve, reject) => {
            const request = indexedDB.open(DB_NAME, DB_VERSION);
            request.onupgradeneeded = () => {
                request.result.createObjectStore(STORE_NAME, { keyPath: 'id' });
            };
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    }
    return dbPromise;
}

function requestToPromise(request) {
    return new Promise((resolve, reject) => {
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

function transactionComplete(transaction) {
    return new Promise((resolve, reject) => {
        transaction.oncomplete = () => resolve();
        transaction.onerror = () => reject(transaction.error);
    });
}

export async function listBoards() {
    const db = await openDatabase();
    const store = db.transaction(STORE_NAME, 'readonly').objectStore(STORE_NAME);
    const records = await requestToPromise(store.getAll());
    return records.map(r => ({ id: r.id, name: r.name, createdAt: r.createdAt, updatedAt: r.updatedAt }));
}

export async function getBoard(id) {
    const db = await openDatabase();
    const store = db.transaction(STORE_NAME, 'readonly').objectStore(STORE_NAME);
    const record = await requestToPromise(store.get(id));
    return record ?? null;
}

export async function createBoard(id, name, createdAt, updatedAt, boardJson) {
    const db = await openDatabase();
    const transaction = db.transaction(STORE_NAME, 'readwrite');
    transaction.objectStore(STORE_NAME).add({ id, name, createdAt, updatedAt, boardJson });
    await transactionComplete(transaction);
}

export async function saveBoard(id, boardJson, updatedAt) {
    const db = await openDatabase();
    const transaction = db.transaction(STORE_NAME, 'readwrite');
    const store = transaction.objectStore(STORE_NAME);
    const existing = await requestToPromise(store.get(id));
    if (!existing) {
        throw new Error(`No board found with id '${id}'.`);
    }
    store.put({ ...existing, boardJson, updatedAt });
    await transactionComplete(transaction);
}

export async function renameBoard(id, name, updatedAt) {
    const db = await openDatabase();
    const transaction = db.transaction(STORE_NAME, 'readwrite');
    const store = transaction.objectStore(STORE_NAME);
    const existing = await requestToPromise(store.get(id));
    if (!existing) {
        throw new Error(`No board found with id '${id}'.`);
    }
    store.put({ ...existing, name, updatedAt });
    await transactionComplete(transaction);
}

export async function deleteBoard(id) {
    const db = await openDatabase();
    const transaction = db.transaction(STORE_NAME, 'readwrite');
    transaction.objectStore(STORE_NAME).delete(id);
    await transactionComplete(transaction);
}
