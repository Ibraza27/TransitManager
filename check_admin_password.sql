-- Script pour vérifier l'utilisateur admin
SELECT
    "Id",
    "Email",
    "NomUtilisateur",
    "Actif",
    "TentativesConnexionEchouees",
    "DateVerrouillage",
    "DoitChangerMotDePasse",
    "MotDePasseHash"
FROM "Utilisateurs"
WHERE "Email" = 'admin@transitmanager.com';
