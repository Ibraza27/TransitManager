# Transit Manager

## 📦 Système de Gestion de Transit International

Transit Manager est une application desktop complète développée en C# .NET 8 avec WPF pour la gestion des opérations de transit international, incluant la gestion des clients, colis, conteneurs et paiements.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=.net)
![WPF](https://img.shields.io/badge/WPF-Windows-0078D4?style=flat-square&logo=windows)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-14+-336791?style=flat-square&logo=postgresql)
![License](https://img.shields.io/badge/License-MIT-green.svg?style=flat-square)

## 🚀 Fonctionnalités Principales

### 👥 Gestion des Clients
- Fiche client complète avec informations détaillées
- Recherche multicritères avec autocomplétion
- Historique complet des envois
- Gestion de la fidélité et des remises
- Export Excel des données clients

### 📦 Gestion des Colis/Marchandises
- Enregistrement avec génération automatique de codes-barres
- Scan de codes-barres en temps réel
- Suivi du statut et de la localisation
- Gestion des colis fragiles et spéciaux
- Impression d'étiquettes personnalisées

### 🚢 Gestion des Conteneurs/Dossiers
- Création et suivi des dossiers d'expédition
- Groupage intelligent des colis
- Calcul automatique du taux de remplissage
- Génération de manifestes d'expédition
- Suivi en temps réel du statut

### 💰 Gestion Financière
- Facturation automatique
- Suivi des paiements (multi-modes)
- Gestion des impayés et relances
- Génération de reçus
- Rapports financiers détaillés

### 📊 Tableau de Bord
- Statistiques en temps réel
- Graphiques interactifs (LiveCharts)
- Alertes et notifications
- Export PDF des rapports

### 🔐 Sécurité et Multi-utilisateurs
- Authentification sécurisée (BCrypt)
- Gestion des rôles et permissions
- Audit trail complet
- Sauvegarde automatique

## 🛠️ Technologies Utilisées

### Backend
- **Framework**: .NET 8.0
- **ORM**: Entity Framework Core 8
- **Base de données**: PostgreSQL 14+
- **Architecture**: Clean Architecture avec patterns Repository et Service

### Frontend
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Design**: Material Design + MahApps.Metro
- **MVVM**: CommunityToolkit.Mvvm
- **Graphiques**: LiveCharts2

### Bibliothèques Clés
- **Codes-barres**: ZXing.Net
- **Documents**: QuestPDF, ClosedXML
- **Sécurité**: BCrypt.Net
- **Logging**: Serilog
- **Temps réel**: SignalR

## 📋 Prérequis

- Windows 10/11 (64-bit)
- .NET 8.0 SDK ou Runtime
- PostgreSQL 14 ou supérieur
- Visual Studio 2022 (recommandé) ou VS Code
- 4 GB RAM minimum (8 GB recommandé)
- 500 MB d'espace disque

## 🔧 Installation

### 1. Cloner le repository
```bash
git clone https://github.com/votre-repo/transit-manager.git
cd transit-manager
```

### 2. Configurer la base de données
```sql
-- Créer la base de données
CREATE DATABASE TransitManager;

-- Créer l'utilisateur (optionnel)
CREATE USER transituser WITH PASSWORD 'votre_mot_de_passe';
GRANT ALL PRIVILEGES ON DATABASE TransitManager TO transituser;
```

### 3. Configurer la chaîne de connexion
Modifier le fichier `src/TransitManager.WPF/appsettings.json` :
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=TransitManager;Username=transituser;Password=votre_mot_de_passe"
  }
}
```

### 4. Installer Entity Framework Core Tools (si nécessaire)
```bash
dotnet tool install --global dotnet-ef
```

### 5. Générer et appliquer les migrations

#### Option 1 : Utiliser le script PowerShell (Windows)
```powershell
.\GenerateMigrations.ps1
```

#### Option 2 : Commandes manuelles
```bash
# Se déplacer dans le dossier Infrastructure
cd src/TransitManager.Infrastructure

# Générer la migration
dotnet ef migrations add InitialCreate --startup-project ../TransitManager.WPF --context TransitContext --output-dir Data/Migrations

# Appliquer la migration
dotnet ef database update --startup-project ../TransitManager.WPF --context TransitContext

# Revenir à la racine
cd ../..
```

### 6. Compiler et lancer
```bash
dotnet build
dotnet run --project src/TransitManager.WPF/TransitManager.WPF.csproj
```

## 🚀 Démarrage Rapide

### Connexion par défaut
- **Utilisateur**: `admin`
- **Mot de passe**: `Admin@123`

### Premier démarrage
1. Connectez-vous avec les identifiants par défaut
2. Changez immédiatement le mot de passe administrateur
3. Créez les utilisateurs nécessaires
4. Configurez les paramètres de l'entreprise dans Administration

### Workflow typique
1. **Créer un client** : Menu Clients → Nouveau Client
2. **Enregistrer un colis** : Menu Marchandises → Nouveau Colis ou Scanner
3. **Créer un conteneur** : Menu Conteneurs → Nouveau Conteneur
4. **Affecter les colis** : Sélectionner les colis → Affecter au conteneur
5. **Générer les documents** : Conteneur → Imprimer Manifeste

## 📁 Structure du Projet

```
TransitManager/
├── src/
│   ├── TransitManager.Core/          # Logique métier
│   │   ├── Entities/                 # Entités du domaine
│   │   ├── Enums/                    # Énumérations
│   │   ├── Interfaces/               # Contrats
│   │   └── Services/                 # Services métier
│   │
│   ├── TransitManager.Infrastructure/ # Accès données
│   │   ├── Data/                     # DbContext et configs
│   │   ├── Repositories/             # Implémentation repositories
│   │   └── Services/                 # Services infrastructure
│   │
│   └── TransitManager.WPF/           # Interface utilisateur
│       ├── Views/                    # Vues XAML
│       ├── ViewModels/               # ViewModels MVVM
│       ├── Controls/                 # Contrôles personnalisés
│       ├── Converters/               # Convertisseurs WPF
│       └── Resources/                # Ressources (styles, images)
│
├── tests/                            # Tests unitaires
├── docs/                             # Documentation
└── scripts/                          # Scripts utilitaires
```

## 🎨 Captures d'écran

### Tableau de bord
![Dashboard](docs/images/dashboard.png)

### Gestion des clients
![Clients](docs/images/clients.png)

### Scanner de codes-barres
![Scanner](docs/images/scanner.png)

## 🔒 Sécurité

### Authentification
- Mots de passe hashés avec BCrypt (coût 12)
- Verrouillage après 5 tentatives échouées
- Expiration de session configurable

### Autorisations
- **Administrateur** : Accès complet
- **Gestionnaire** : Création/modification
- **Opérateur** : Saisie et consultation
- **Comptable** : Module financier uniquement
- **Invité** : Lecture seule

### Audit
- Traçabilité complète des actions
- Historique des modifications
- Logs détaillés avec Serilog

## 🔄 Sauvegarde et Restauration

### Sauvegarde automatique
- Configurée par défaut toutes les 24h
- Rétention de 30 jours
- Compression ZIP

### Sauvegarde manuelle
```csharp
// Via l'interface
Administration → Sauvegarde → Créer une sauvegarde

// Via le code
await backupService.CreateBackupAsync();
```

## 🐛 Dépannage

### Problèmes courants

**Erreur de connexion à la base de données**
- Vérifier que PostgreSQL est démarré
- Vérifier la chaîne de connexion
- Vérifier les droits de l'utilisateur

**Erreur lors de la génération des migrations**
```bash
# Installer Entity Framework Core Tools
dotnet tool install --global dotnet-ef

# Vérifier l'installation
dotnet ef --version
```

**Application lente**
- Vérifier les index de base de données
- Activer la virtualisation dans les DataGrids
- Réduire le nombre d'enregistrements affichés

**Erreur de scan de codes-barres**
- Vérifier les permissions caméra
- Installer les drivers de la caméra
- Tester avec la saisie manuelle

## 📚 Documentation Technique

### API des Services

#### ClientService
```csharp
// Rechercher des clients
var clients = await clientService.SearchAsync("dupont");

// Créer un client
var client = new Client { Nom = "Dupont", Prenom = "Jean" };
await clientService.CreateAsync(client);
```

#### ColisService
```csharp
// Scanner un colis
var colis = await colisService.ScanAsync("123456789", "Entrepôt A");

// Affecter à un conteneur
await colisService.AssignToConteneurAsync(colisId, conteneurId);
```

### Extension du système

Pour ajouter un nouveau module :
1. Créer les entités dans `Core/Entities`
2. Ajouter les services dans `Core/Services`
3. Créer les configurations EF dans `Infrastructure/Data/Configurations`
4. Créer les vues dans `WPF/Views`
5. Implémenter les ViewModels
6. Ajouter les migrations EF

## 🤝 Contribution

Les contributions sont les bienvenues ! Pour contribuer :

1. Fork le projet
2. Créer une branche (`git checkout -b feature/AmazingFeature`)
3. Commit les changements (`git commit -m 'Add AmazingFeature'`)
4. Push la branche (`git push origin feature/AmazingFeature`)
5. Ouvrir une Pull Request

### Standards de code
- Suivre les conventions C# de Microsoft
- Documenter le code avec des commentaires XML
- Écrire des tests unitaires
- Respecter l'architecture Clean

## 📄 Licence

Ce projet est sous licence MIT. Voir le fichier [LICENSE](LICENSE) pour plus de détails.

## 👥 Équipe

- **Chef de projet** : [Votre nom]
- **Développeur principal** : [Nom]
- **UI/UX Designer** : [Nom]

## 📞 Support

- **Email** : support@transitmanager.com
- **Documentation** : [https://docs.transitmanager.com](https://docs.transitmanager.com)
- **Issues** : [GitHub Issues](https://github.com/votre-repo/transit-manager/issues)

## 🙏 Remerciements

- Material Design in XAML pour les composants UI
- MahApps.Metro pour le thème moderne
- La communauté .NET pour les nombreuses bibliothèques open source

---

© 2024 Transit Manager. Tous droits réservés.