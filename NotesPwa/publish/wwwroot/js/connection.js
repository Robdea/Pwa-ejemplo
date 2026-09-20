// Detección Online/Offline (navigator.onLine) y vigilancia de disponibilidad del backend.

let serverWatchId = null;

export function initConnectionStatus(dotNetHelper) {
  syncStatus(dotNetHelper);
  window.addEventListener('online', () => syncStatus(dotNetHelper));
  window.addEventListener('offline', () => syncStatus(dotNetHelper));
}

// Comprueba cada intervalMs si el backend responde y avisa a .NET (OnServerReachable).
export function initServerWatch(dotNetHelper, apiBaseUrl, intervalMs) {
  const check = async () => {
    try {
      await fetch(apiBaseUrl + '/api/health', { cache: 'no-store' });
      dotNetHelper.invokeMethodAsync('OnServerReachable', true);
    } catch {
      dotNetHelper.invokeMethodAsync('OnServerReachable', false);
    }
  };

  check();
  serverWatchId = setInterval(check, intervalMs || 3000);
}

export function stopServerWatch() {
  if (serverWatchId) {
    clearInterval(serverWatchId);
    serverWatchId = null;
  }
}

function syncStatus(dotNetHelper) {
  dotNetHelper.invokeMethodAsync('OnConnectionChanged', navigator.onLine);
}