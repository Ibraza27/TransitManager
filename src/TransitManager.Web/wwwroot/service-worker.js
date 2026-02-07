self.addEventListener('install', event => {
    console.log('[Service Worker] Install');
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    console.log('[Service Worker] Activate');
});

self.addEventListener('push', event => {
    console.log('[Service Worker] Push Received.');
    const payload = event.data ? event.data.json() : { title: 'Transit Manager', body: 'Nouvelle notification', icon: 'favicon.jpg' };
    
    const options = {
        body: payload.body,
        icon: payload.icon || 'favicon.jpg',
        badge: payload.badge || 'favicon.jpg',
        data: payload.data,
        vibrate: [100, 50, 100]
    };

    event.waitUntil(
        self.registration.showNotification(payload.title, options)
    );
});

self.addEventListener('notificationclick', event => {
    console.log('[Service Worker] Notification click received.');
    event.notification.close();

    const urlToOpen = event.notification.data?.url || '/';

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true }).then(windowClients => {
            // Check if there is already a window/tab open with the target URL
            for (let i = 0; i < windowClients.length; i++) {
                const client = windowClients[i];
                if (client.url === urlToOpen && 'focus' in client) {
                    return client.focus();
                }
            }
            // If not, open a new window
            if (clients.openWindow) {
                return clients.openWindow(urlToOpen);
            }
        })
    );
});
