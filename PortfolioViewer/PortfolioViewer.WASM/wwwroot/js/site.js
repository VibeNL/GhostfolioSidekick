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
globalThis.forceBlazorReload = () => __awaiter(void 0, void 0, void 0, function* () {
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
export {};
