// Module JavaScript pour gérer les Push Notifications côté client (Blazor Interop)

window.pushNotifications = {

    // Vérifie si le navigateur supporte les push notifications
    isSupported: function () {
        return ('serviceWorker' in navigator) && ('PushManager' in window) && ('Notification' in window);
    },

    // Vérifie le statut actuel de la permission
    getPermissionStatus: function () {
        if (!this.isSupported()) return 'unsupported';
        return Notification.permission; // 'default', 'granted', 'denied'
    },

    // Enregistre le service worker
    registerServiceWorker: async function () {
        if (!('serviceWorker' in navigator)) return null;
        try {
            const registration = await navigator.serviceWorker.register('/service-worker.js', { scope: '/' });
            console.log('[Push] Service Worker enregistré:', registration.scope);
            return true;
        } catch (error) {
            console.error('[Push] Erreur enregistrement SW:', error);
            return false;
        }
    },

    // S'abonner aux push notifications
    subscribe: async function (vapidPublicKey) {
        try {
            const registration = await navigator.serviceWorker.ready;

            // Vérifier si déjà abonné
            let subscription = await registration.pushManager.getSubscription();
            if (subscription) {
                console.log('[Push] Déjà abonné, renvoi de l\'abonnement existant');
                return JSON.stringify(subscription);
            }

            // Convertir la clé VAPID en Uint8Array
            const applicationServerKey = this._urlBase64ToUint8Array(vapidPublicKey);

            // Demander la permission et s'abonner
            subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: applicationServerKey
            });

            console.log('[Push] Abonnement réussi:', subscription.endpoint);
            return JSON.stringify(subscription);
        } catch (error) {
            console.error('[Push] Erreur abonnement:', error);
            return null;
        }
    },

    // Se désabonner
    unsubscribe: async function () {
        try {
            const registration = await navigator.serviceWorker.ready;
            const subscription = await registration.pushManager.getSubscription();

            if (subscription) {
                const endpoint = subscription.endpoint;
                await subscription.unsubscribe();
                console.log('[Push] Désabonné avec succès');
                return endpoint;
            }
            return null;
        } catch (error) {
            console.error('[Push] Erreur désabonnement:', error);
            return null;
        }
    },

    // Vérifie si l'utilisateur est actuellement abonné
    isSubscribed: async function () {
        try {
            const registration = await navigator.serviceWorker.ready;
            const subscription = await registration.pushManager.getSubscription();
            return subscription !== null;
        } catch {
            return false;
        }
    },

    // Utilitaire : convertit une clé base64url en Uint8Array
    _urlBase64ToUint8Array: function (base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding)
            .replace(/-/g, '+')
            .replace(/_/g, '/');

        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);

        for (let i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray;
    }
};
