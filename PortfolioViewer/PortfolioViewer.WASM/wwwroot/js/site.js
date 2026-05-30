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
function scrollToBottom(id) {
    const element = document.getElementById(id);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}
// Explicitly define and assign the function to globalThis
const forceBlazorReload = () => __awaiter(void 0, void 0, void 0, function* () {
    if ('serviceWorker' in navigator) {
        const registrations = yield navigator.serviceWorker.getRegistrations();
        for (const registration of registrations) {
            yield registration.unregister();
        }
    }
    // Clear all browser caches
    if ('caches' in globalThis) {
        const cacheNames = yield caches.keys();
        for (const name of cacheNames) {
            yield caches.delete(name);
        }
    }
    globalThis.location.reload();
});
globalThis.forceBlazorReload = forceBlazorReload;

// CSV download helper - triggers a browser file download with the given CSV content
function downloadCsv(filename, csvContent) {
    const bom = '\uFEFF'; // UTF-8 BOM for Excel compatibility
    const blob = new Blob([bom + csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}
globalThis.downloadCsv = downloadCsv;
