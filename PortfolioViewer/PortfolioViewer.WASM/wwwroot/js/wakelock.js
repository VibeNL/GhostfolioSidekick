// Wake Lock API to prevent screen from sleeping during sync
let wakeLock = null;

export async function requestWakeLock() {
    try {
        if ('wakeLock' in navigator) {
            wakeLock = await navigator.wakeLock.request('screen');
            console.log('Wake lock is active');
            
            // Handle wake lock release when page becomes hidden
            wakeLock.addEventListener('release', () => {
                console.log('Wake lock has been released');
            });
            
            return true;
        } else {
            console.warn('Wake Lock API not supported');
            return false;
        }
    } catch (err) {
        console.error(`Failed to request wake lock: ${err.name}, ${err.message}`);
        return false;
    }
}

export async function releaseWakeLock() {
    try {
        if (wakeLock !== null) {
            await wakeLock.release();
            wakeLock = null;
            console.log('Wake lock released manually');
            return true;
        }
        return false;
    } catch (err) {
        console.error(`Failed to release wake lock: ${err.name}, ${err.message}`);
        return false;
    }
}

export function isWakeLockSupported() {
    return 'wakeLock' in navigator;
}

export function isWakeLockActive() {
    return wakeLock !== null && !wakeLock.released;
}

// Re-request wake lock when page becomes visible again (if it was active before)
document.addEventListener('visibilitychange', async () => {
    if (wakeLock !== null && wakeLock.released && document.visibilityState === 'visible') {
        try {
            wakeLock = await navigator.wakeLock.request('screen');
            console.log('Wake lock re-acquired after page became visible');
        } catch (err) {
            console.error(`Failed to re-acquire wake lock: ${err.name}, ${err.message}`);
        }
    }
});