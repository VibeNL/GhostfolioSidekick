"use strict";
function scrollToBottom(id) {
	const element = document.getElementById(id);
	if (element) {
		element.scrollTop = element.scrollHeight;
	}
}

// Unregister service workers and force reload to ensure new version loads
globalThis.forceBlazorReload = async function () {
	if ('serviceWorker' in navigator) {
		const registrations = await navigator.serviceWorker.getRegistrations();
		for (let registration of registrations) {
			await registration.unregister();
		}
	}

	// Clear all browser caches
	if (globalThis.caches) {
		const cacheNames = await caches.keys();
		for (const name of cacheNames) {
			await caches.delete(name);
		}
	}

	globalThis.location.reload();
};