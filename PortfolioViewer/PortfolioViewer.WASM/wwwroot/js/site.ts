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

// CSV download helper for CsvExportService JS interop
const downloadCsv = (fileName: string, csvContent: string): void => {
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};

(globalThis as any).downloadCsv = downloadCsv;
