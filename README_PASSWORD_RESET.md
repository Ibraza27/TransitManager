# Guide de réinitialisation du mot de passe admin

## Problème identifié

Le hash du mot de passe admin dans la base de données ne correspond pas au mot de passe "Admin@123" attendu.

## Solution rapide (Recommandée)

Exécutez le script SQL `reset_admin_password.sql` avec votre outil PostgreSQL préféré (pgAdmin, psql, etc.) :

```bash
psql -h localhost -U postgres -d TransitManager -f reset_admin_password.sql
```

Ou copiez/collez le contenu du fichier `reset_admin_password.sql` dans pgAdmin.

## Solution alternative 1 : Avec dotnet

Si vous avez dotnet installé, vous pouvez exécuter :

```bash
dotnet run --project ResetAdminPassword.csproj
```

## Solution alternative 2 : Avec une migration

Une nouvelle migration sera créée pour corriger le problème de manière permanente. Pour l'appliquer :

```bash
cd src/TransitManager.Infrastructure
dotnet ef database update
```

## Détails techniques

### Hash BCrypt utilisé
- **Mot de passe** : `Admin@123`
- **Hash BCrypt** : `$2a$11$47CimAPLqf80X5ildRmPXuC0TWgjvHAIA7CeifbveROmjA1zR0dOu`

Ce hash a été généré lors de la migration initiale et est maintenant fixé dans le code pour éviter toute incohérence future.

### Changements effectués

1. **TransitContext.cs** : Le hash du mot de passe admin est maintenant fixe au lieu d'être généré dynamiquement
2. **Migration** : Une nouvelle migration sera créée pour mettre à jour la base de données
3. **Scripts SQL** : Scripts fournis pour une réinitialisation manuelle rapide

## Vérification

Après avoir appliqué la correction, vous devriez pouvoir vous connecter avec :

- **Email** : `admin@transitmanager.com`
- **Mot de passe** : `Admin@123`

## Prévention

Le problème était causé par l'utilisation de `BCrypt.HashPassword()` directement dans `HasData()`, ce qui générait un nouveau hash à chaque fois. Le hash est maintenant fixe dans le code.
