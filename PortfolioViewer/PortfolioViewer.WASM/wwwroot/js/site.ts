function scrollToBottom(id: string): void {
    const element = document.getElementById(id);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}

// Explicitly define and assign the function to globalThis
const forceBlazorReload = async (): Promise<void> => {
    if ('serviceWorker' in navigator) {
        const registrations = await navigator.serviceWorker.getRegistrations();
        for (const registration of registrations) {
            await registration.unregister();
        }
    }

    // Clear all browser caches
    if ('caches' in globalThis) {
        const cacheNames = await caches.keys();
        for (const name of cacheNames) {
            await caches.delete(name);
        }
    }

    globalThis.location.reload();
};

(globalThis as any).forceBlazorReload = forceBlazorReload;
