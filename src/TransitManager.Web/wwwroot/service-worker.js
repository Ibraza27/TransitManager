// Service Worker pour TransitManager - Web Push Notifications

const CACHE_NAME = 'transitmanager-v1';

// Installation : on ne cache que le minimum nécessaire pour le push
self.addEventListener('install', (event) => {
    console.log('[SW] Installation du Service Worker');
    self.skipWaiting(); // Activation immédiate
});

// Activation : nettoyage des anciens caches
self.addEventListener('activate', (event) => {
    console.log('[SW] Service Worker activé');
    event.waitUntil(
        caches.keys().then((cacheNames) => {
            return Promise.all(
                cacheNames
                    .filter((name) => name !== CACHE_NAME)
                    .map((name) => caches.delete(name))
            );
        }).then(() => self.clients.claim()) // Prend le contrôle immédiatement
    );
});

// Réception d'une notification push
self.addEventListener('push', (event) => {
    console.log('[SW] Push reçu:', event);

    let data = {
        title: 'Transit Manager',
        body: 'Nouvelle notification',
        icon: '/images/logo.jpg',
        badge: '/images/logo.jpg',
        url: '/'
    };

    if (event.data) {
        try {
            data = { ...data, ...event.data.json() };
        } catch (e) {
            console.error('[SW] Erreur parsing push data:', e);
            data.body = event.data.text();
        }
    }

    const options = {
        body: data.body,
        icon: data.icon,
        badge: data.badge,
        vibrate: [200, 100, 200], // Vibration mobile
        data: {
            url: data.url,
            timestamp: data.timestamp || Date.now()
        },
        actions: [
            { action: 'open', title: 'Ouvrir' },
            { action: 'close', title: 'Fermer' }
        ],
        requireInteraction: false, // Se ferme automatiquement
        tag: 'transitmanager-' + Date.now(), // Évite les duplications
        renotify: true
    };

    event.waitUntil(
        self.registration.showNotification(data.title, options)
    );
});

// Clic sur une notification
self.addEventListener('notificationclick', (event) => {
    console.log('[SW] Clic sur notification:', event.action);
    event.notification.close();

    if (event.action === 'close') return;

    const targetUrl = event.notification.data?.url || '/';

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clientList) => {
            // Chercher une fenêtre existante de l'app
            for (const client of clientList) {
                if (client.url.includes(self.location.origin) && 'focus' in client) {
                    client.navigate(targetUrl);
                    return client.focus();
                }
            }
            // Sinon ouvrir une nouvelle fenêtre
            return clients.openWindow(targetUrl);
        })
    );
});

// Fermeture d'une notification (swipe)
self.addEventListener('notificationclose', (event) => {
    console.log('[SW] Notification fermée par l\'utilisateur');
});
