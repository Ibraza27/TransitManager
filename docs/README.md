Transit Manager
📦 Système de Gestion de Transit International

Transit Manager est une application de bureau complète développée en C# avec .NET 8 et WPF. Elle est conçue pour gérer toutes les facettes des opérations de transit international, incluant la gestion des clients, des colis (avec inventaire détaillé), des véhicules, des conteneurs, et des paiements.

![alt text](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=.net)

![alt text](https://img.shields.io/badge/WPF-Windows-0078D4?style=flat-square&logo=windows)

![alt text](https://img.shields.io/badge/PostgreSQL-14+-336791?style=flat-square&logo=postgresql)

![alt text](https://img.shields.io/badge/License-MIT-green.svg?style=flat-square)

🚀 Fonctionnalités Principales
👥 Gestion des Clients

    Fiche client complète avec informations détaillées.

    Recherche multicritères et filtres dynamiques (par statut, ville, etc.).

    Historique complet des envois (colis et véhicules).

    Gestion de la fidélité et des remises.

    Export Excel des données clients.

📦 Gestion des Colis

    Enregistrement détaillé avec multiples codes-barres.

    Inventaire détaillé par colis (désignation, quantité, valeur).

    Calcul automatique du nombre de pièces et de la valeur déclarée à partir de l'inventaire.

    Scan de codes-barres en temps réel via caméra.

    Suivi intelligent du statut (synchronisé avec le conteneur ou manuel pour les exceptions).

    Gestion des colis fragiles et spéciaux.

    Impression d'étiquettes personnalisées.

🚗 Gestion des Véhicules

    Fiche véhicule complète (immatriculation, marque, modèle, etc.).

    État des lieux visuel avec marquage des impacts et rayures.

    Suivi du statut (synchronisé ou manuel).

    Affectation à des conteneurs.

    Recherche et filtres multicritères.

🚢 Gestion des Conteneurs

    Création et suivi des dossiers d'expédition de bout en bout.

    Groupage intelligent des colis et véhicules.

    Gestion de statut avancée : se met à jour automatiquement en fonction des dates, de l'état de son contenu (Problème, Livré) ou de sa vacuité.

    Génération de manifestes d'expédition.

    Suivi en temps réel du statut.

💰 Gestion Financière

    Facturation automatique.

    Suivi des paiements (multi-modes).

    Gestion des impayés et relances.

    Génération de reçus.

    Rapports financiers détaillés.

📊 Tableau de Bord

    Statistiques en temps réel sur les clients, colis, et finances.

    Graphiques interactifs (LiveCharts) pour visualiser l'activité.

    Alertes et notifications pour les actions importantes.

    Export PDF des rapports.

🔐 Sécurité et Multi-utilisateurs

    Authentification sécurisée (BCrypt).

    Gestion des rôles et permissions.

    Audit trail complet des actions des utilisateurs.

    Sauvegarde automatique et restauration de la base de données.

🛠️ Technologies Utilisées
Backend

    Framework: .NET 8.0

    ORM: Entity Framework Core 8

    Base de données: PostgreSQL 14+

    Architecture: Clean Architecture avec patterns Repository et Service.

Frontend

    UI Framework: WPF (Windows Presentation Foundation)

    Design: Material Design + MahApps.Metro

    MVVM: CommunityToolkit.Mvvm

    Graphiques: LiveCharts2

Bibliothèques Clés

    Codes-barres: ZXing.Net

    Documents PDF/Excel: QuestPDF, ClosedXML

    Sécurité: BCrypt.Net

    Logging: Serilog

    Interactivité UI: Microsoft.Xaml.Behaviors.Wpf

📋 Prérequis

    Windows 10/11 (64-bit)

    .NET 8.0 SDK ou Runtime

    PostgreSQL 14 ou supérieur

    Visual Studio 2022 (recommandé) ou VS Code

    4 Go de RAM minimum (8 Go recommandés)

    500 Mo d'espace disque

🔧 Installation
1. Cloner le repository
code Bash
IGNORE_WHEN_COPYING_START
IGNORE_WHEN_COPYING_END

    
git clone https://github.com/votre-repo/transit-manager.git
cd transit-manager

  

2. Configurer la base de données
code SQL
IGNORE_WHEN_COPYING_START
IGNORE_WHEN_COPYING_END

    
-- Créer la base de données
CREATE DATABASE TransitManager;

-- Créer l'utilisateur (optionnel mais recommandé)
CREATE USER transituser WITH PASSWORD 'votre_mot_de_passe';
GRANT ALL PRIVILEGES ON DATABASE TransitManager TO transituser;

  

3. Configurer la chaîne de connexion

Modifier le fichier src/TransitManager.WPF/appsettings.json :
code JSON
IGNORE_WHEN_COPYING_START
IGNORE_WHEN_COPYING_END

    
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=TransitManager;Username=transituser;Password=votre_mot_de_passe"
  }
}```

### 4. Restaurer les outils .NET
Cette commande est cruciale car elle installe `dotnet-ef` localement pour le projet.
```bash
# À la racine du projet (où se trouve le fichier .sln)
dotnet tool restore

  

5. Générer et appliquer les migrations
code Bash
IGNORE_WHEN_COPYING_START
IGNORE_WHEN_COPYING_END

    
# Se déplacer dans le dossier Infrastructure
cd src/TransitManager.Infrastructure

# Créer la migration initiale
dotnet ef migrations add InitialCreate

# Appliquer la migration à la base de données
dotnet ef database update

# Revenir à la racine
cd ../..

  

6. Compiler et lancer
code Bash
IGNORE_WHEN_COPYING_START
IGNORE_WHEN_COPYING_END

    
dotnet build
dotnet run --project src/TransitManager.WPF/TransitManager.WPF.csproj

  

🚀 Démarrage Rapide
Connexion par défaut

    Utilisateur: admin

    Mot de passe: Admin@123

Premier démarrage

    Connectez-vous avec les identifiants par défaut.

    Changez immédiatement le mot de passe de l'administrateur.

    Créez les utilisateurs nécessaires et assignez-leur des rôles.

    Configurez les paramètres de l'entreprise dans la section Administration.

Workflow typique

    Créer un client : Menu Clients → Nouveau Client.

    Enregistrer un véhicule : Menu Véhicules → Nouveau Véhicule.

    Enregistrer un colis : Menu Colis → Nouveau Colis.

        Optionnel : Cliquer sur Inventaire pour détailler le contenu du colis.

    Créer un conteneur : Menu Conteneurs → Nouveau Conteneur.

    Affecter les colis/véhicules : Depuis la fiche du conteneur, rechercher et ajouter les éléments.

    Mettre à jour les statuts : Mettre à jour les dates du conteneur (départ, arrivée...) pour synchroniser le statut de tout son contenu.

    Générer les documents : Depuis la fiche du conteneur, imprimer le manifeste.

📁 Structure du Projet
code Code
IGNORE_WHEN_COPYING_START
IGNORE_WHEN_COPYING_END

    
TransitManager/
├── .config/
│   └── dotnet-tools.json           # Outils .NET locaux (dotnet-ef)
├── src/
│   ├── TransitManager.Core/          # Logique métier, entités, interfaces
│   ├── TransitManager.Infrastructure/ # Accès aux données, services externes
│   └── TransitManager.WPF/           # Interface utilisateur (Vues, ViewModels)
├── docs/                             # Documentation et images
└── TransitManager.sln                # Fichier de solution Visual Studio

  

🎨 Captures d'écran
Tableau de bord

![alt text](docs/images/dashboard.png)

Gestion des colis avec inventaire

![alt text](docs/images/inventaire.png)

État des lieux d'un véhicule

![alt text](docs/images/etat-des-lieux.png)

🔒 Sécurité
Authentification

    Mots de passe hashés avec BCrypt (coût 12).

    Verrouillage du compte après 5 tentatives de connexion échouées.

    Expiration de session configurable.

Autorisations

    Administrateur : Accès complet.

    Gestionnaire : Création/modification des dossiers.

    Opérateur : Saisie et consultation des données.

    Comptable : Accès au module financier uniquement.

    Invité : Lecture seule.

Audit

    Traçabilité complète des actions des utilisateurs (création, modification, suppression).

    Logs détaillés avec Serilog.

🔄 Sauvegarde et Restauration
Sauvegarde automatique

    Configurée par défaut toutes les 24h.

    Rétention des sauvegardes sur 30 jours.

    Compression des sauvegardes au format ZIP.

Sauvegarde manuelle
code C#
IGNORE_WHEN_COPYING_START
IGNORE_WHEN_COPYING_END

    
// Via l'interface
Administration → Sauvegarde → Créer une sauvegarde

// Via le code
await backupService.CreateBackupAsync();

  

🤝 Contribution

Les contributions sont les bienvenues !

    Fork le projet.

    Créez une branche (git checkout -b feature/NouvelleFonctionnalite).

    Commitez vos changements (git commit -m 'Ajout de NouvelleFonctionnalite').

    Pushez la branche (git push origin feature/NouvelleFonctionnalite).

    Ouvrez une Pull Request.

Standards de code

    Suivre les conventions C# de Microsoft.

    Documenter le code avec des commentaires XML.

    Écrire des tests unitaires pour les nouvelles logiques métier.

    Respecter l'architecture MVVM et Clean.

📄 Licence

Ce projet est sous licence MIT. Voir le fichier LICENSE pour plus de détails.
📞 Support

    Email : support@transitmanager.com

    Documentation : https://docs.transitmanager.com

    Issues : GitHub Issues