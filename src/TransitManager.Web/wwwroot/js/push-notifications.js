window.pushNotifications = {
    isSupported: function () {
        return 'serviceWorker' in navigator && 'PushManager' in window;
    },

    registerServiceWorker: async function () {
        if (!this.isSupported()) return;
        try {
            await navigator.serviceWorker.register('service-worker.js');
            console.log('[Push] Service Worker registered');
        } catch (error) {
            console.error('[Push] Service Worker registration failed:', error);
        }
    },

    askPermission: async function () {
        if (!this.isSupported()) return 'default';
        return await Notification.requestPermission();
    },

    getPermissionState: function () {
        if (!this.isSupported()) return 'denied';
        return Notification.permission;
    },

    getSubscription: async function () {
        if (!this.isSupported()) return null;
        const registration = await navigator.serviceWorker.ready;
        return await registration.pushManager.getSubscription();
    },

    subscribe: async function (vapidPublicKey) {
        if (!this.isSupported()) return null;
        const registration = await navigator.serviceWorker.ready;

        const convertedVapidKey = this.urlBase64ToUint8Array(vapidPublicKey);

        const subscription = await registration.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: convertedVapidKey
        });

        return subscription;
    },

    urlBase64ToUint8Array: function (base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding)
            .replace(/\-/g, '+')
            .replace(/_/g, '/');
        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);
        for (let i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray;
    }
};
