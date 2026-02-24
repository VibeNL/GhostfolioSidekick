export function registerServiceWorkerUpdateListener(dotNetHelper) {
	if ('serviceWorker' in navigator) {
		navigator.serviceWorker.addEventListener('message', event => {
			if (event.data && event.data.type === 'SERVICE_WORKER_UPDATED') {
				console.log('Service worker updated to version:', event.data.version);
				dotNetHelper.invokeMethodAsync('OnServiceWorkerUpdated', event.data.version);
			}
		});

		// Check for updates when the page loads
		navigator.serviceWorker.ready.then(registration => {
			registration.update();
		});
	}
}

export function checkForServiceWorkerUpdate() {
	if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
		navigator.serviceWorker.controller.postMessage({ type: 'CHECK_VERSION' });
	}
}
