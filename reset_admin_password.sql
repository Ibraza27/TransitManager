-- Script pour réinitialiser le mot de passe admin
-- IMPORTANT: Ce script réinitialise le mot de passe à "Admin@123"
-- Ce hash BCrypt a été généré et vérifié pour correspondre à "Admin@123"

-- Étape 1: Afficher l'état actuel
SELECT
    "Id",
    "Email",
    "NomUtilisateur",
    "Actif",
    "TentativesConnexionEchouees",
    "DateVerrouillage",
    "DoitChangerMotDePasse"
FROM "Utilisateurs"
WHERE "Email" = 'admin@transitmanager.com';

-- Étape 2: Réinitialiser le mot de passe
-- Le hash ci-dessous correspond au mot de passe "Admin@123" avec BCrypt
UPDATE "Utilisateurs"
SET
    "MotDePasseHash" = '$2a$11$47CimAPLqf80X5ildRmPXuC0TWgjvHAIA7CeifbveROmjA1zR0dOu',
    "TentativesConnexionEchouees" = 0,
    "DateVerrouillage" = NULL,
    "DoitChangerMotDePasse" = false,
    "Actif" = true
WHERE "Email" = 'admin@transitmanager.com';

-- Étape 3: Vérifier que la mise à jour a fonctionné
SELECT
    "Id",
    "Email",
    "NomUtilisateur",
    "Actif",
    "TentativesConnexionEchouees",
    "DateVerrouillage",
    "DoitChangerMotDePasse",
    LENGTH("MotDePasseHash") as "LongueurHash"
FROM "Utilisateurs"
WHERE "Email" = 'admin@transitmanager.com';
