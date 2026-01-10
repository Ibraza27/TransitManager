# Guide: Configuration de la Page de Maintenance (Cloudflare)

Ce guide explique comment afficher une page de "Maintenance en cours" personnalisée au lieu de l'erreur "502 Bad Gateway" lorsque votre serveur redémarre ou est inaccessible.

## Étape 1 : Prérequis
- Avoir accès au tableau de bord Cloudflare pour `hippocampetransitmanager.com`.
- Le fichier `maintenance.html` a été créé dans votre projet local (pour info), mais nous allons l'intégrer directement dans Cloudflare pour qu'il soit toujours disponible, même si votre serveur est éteint.

## Étape 2 : Créer un "Worker" Cloudflare (Gratuit)
Les "Workers" sont des petits scripts qui tournent sur les serveurs de Cloudflare et qui interceptent les requêtes avant qu'elles n'arrivent chez vous.

1.  Connectez-vous à **Cloudflare Dashboard**.
2.  Allez dans **Workers & Pages** (menu latéral gauche).
3.  Cliquez sur **Create Application** -> **Create Worker**.
4.  Sélectionnez l'option **"Commencez avec Hello World !"** (Start with Hello World).
5.  Cliquez sur **Deploy** (bouton bleu) une première fois pour créer le worker.
6.  Une fois créé, cliquez sur **Edit Code**.
7.  **Supprimez tout le code existant** et **Copiez-Collez** le contenu du fichier `src/TransitManager.Web/cloudflare_worker.js` (fourni dans votre projet).
8.  Cliquez sur **Save and Deploy** (en haut à droite).

## Étape 3 : Lier le Worker à votre domaine
Maintenant que le script existe, il faut dire à Cloudflare de l'utiliser pour votre site.

1.  Allez dans votre domaine **hippocampetransitmanager.com** sur Cloudflare.
2.  Allez dans **Workers Routes** (ou "Triggers" si vous êtes dans la vue Worker).
3.  Cliquez sur **Add Route**.
4.  **Route**: `*hippocampetransitmanager.com/*` 
    *   *Cela capture toutes les pages du site.*
5.  **Zone**: Sélectionnez `hippocampetransitmanager.com`.
6.  **Worker**: Sélectionnez `transit-maintenance` (le worker créé à l'étape 2).
7.  Cliquez sur **Save**.

## C'est terminé !
Désormais, à chaque fois que Cloudflare détectera une erreur **502 Bad Gateway** (serveur qui redémarre) ou **504 Timeout**, il affichera automatiquement votre belle page de maintenance bleue au lieu de la page d'erreur grise par défaut.

### Tester
Pour tester, vous pouvez éteindre votre application locale (`taskkill`) tout en laissant le tunnel Cloudflare actif (si applicable) ou simplement observer lors du prochain redémarrage.
