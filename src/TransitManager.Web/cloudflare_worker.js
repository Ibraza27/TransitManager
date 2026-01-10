
// Configuration
const CONFIG = {
    // Replace with your maintenance HTML or fetch it from a public URL if you prefer
    // Using a const string ensures it works even if your origin is completely offline
    MAINTENANCE_PAGE_URL: "https://hippocampetransitmanager.com/maintenance.html",

    // Or embed the HTML directly to ensure 100% availability (Recommended)
    MAINTENANCE_HTML: `
<!DOCTYPE html>
<html lang="fr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Maintenance en cours - TransitManager</title>
    <style>
        :root { --primary: #0d6efd; --secondary: #6c757d; --bg: #f3f4f6; }
        body { font-family: system-ui, sans-serif; background: var(--bg); height: 100vh; margin: 0; display: flex; align-items: center; justify-content: center; text-align: center; }
        .container { background: white; padding: 3rem; border-radius: 1rem; box-shadow: 0 10px 25px rgba(0,0,0,0.05); max-width: 500px; width: 90%; }
        .icon { font-size: 4rem; margin-bottom: 1.5rem; animation: pulse 2s infinite; }
        h1 { font-size: 1.75rem; margin-bottom: 1rem; color: #212529; }
        p { color: var(--secondary); line-height: 1.6; margin-bottom: 2rem; }
        .btn { padding: 0.75rem 1.5rem; background: var(--primary); color: white; border: none; border-radius: 0.5rem; cursor: pointer; }
        @keyframes pulse { 0% { opacity: 1; } 50% { opacity: 0.7; } 100% { opacity: 1; } }
    </style>
</head>
<body>
    <div class="container">
        <div class="icon">🛠️</div>
        <h1>Maintenance en cours</h1>
        <p>Nous effectuons une mise à jour. L'application sera disponible dans quelques instants.</p>
        <button onclick="window.location.reload()" class="btn">Réessayer</button>
    </div>
    <script>
        setTimeout(() => location.reload(), 15000);
    </script>
</body>
</html>`
};

export default {
    async fetch(request, env, ctx) {
        try {
            // Pass the request to the origin
            const response = await fetch(request);

            // Check for specific error codes indicative of "Server Down"
            // 502: Bad Gateway (Cloudflare/Server connection issue)
            // 503: Service Unavailable
            // 504: Gateway Timeout
            // 521: Web Server Is Down (Cloudflare specific)
            // 522: Connection Timed Out (Cloudflare specific)
            // 523: Origin Unreachable
            const errorCodes = [502, 503, 504, 521, 522, 523];

            if (errorCodes.includes(response.status)) {
                return new Response(CONFIG.MAINTENANCE_HTML, {
                    headers: {
                        "content-type": "text/html;charset=UTF-8",
                        "cache-control": "no-store" // Don't cache the error page
                    },
                    status: 503 // Service Unavailable implies temporary condition
                });
            }

            return response;
        } catch (e) {
            // If the fetch itself throws (network error, total failure), serve maintenance page
            return new Response(CONFIG.MAINTENANCE_HTML, {
                headers: { "content-type": "text/html;charset=UTF-8" },
                status: 503
            });
        }
    },
};
