# TransitManager - Comprehensive Architecture Overview

## Executive Summary

TransitManager is a comprehensive logistics and transit management system built with modern .NET technologies. The solution currently consists of a RESTful API backend, Windows desktop application (WPF), and Android mobile application (.NET MAUI). This document provides a detailed architectural analysis to support planning a new web version with role-based access control.

---

## 1. Project Structure & Technology Stack

### 1.1 Solution Architecture

```
TransitManager/
├── src/
│   ├── TransitManager.API               (ASP.NET Core 8.0 REST API)
│   ├── TransitManager.Core              (Domain models & interfaces)
│   ├── TransitManager.Infrastructure    (Data access & services)
│   ├── TransitManager.WPF               (Windows desktop client)
│   └── TransitManager.Mobile            (.NET MAUI - Android client)
├── docs/                                (Documentation)
└── TransitManager.sln
```

### 1.2 Technology Stack

| Layer | Technologies |
|-------|--------------|
| **Framework & Runtime** | .NET 8.0 |
| **Web API** | ASP.NET Core 8.0 (REST + SignalR) |
| **ORM & Database** | Entity Framework Core 8.0, PostgreSQL 8.0+ |
| **Desktop UI** | WPF (.NET 8.0-windows) |
| **Mobile UI** | .NET MAUI (Android net8.0-android) |
| **Authentication** | BCrypt.Net-Next 4.0.3 |
| **API Client** | Refit 7.0.0 (Mobile) |
| **Real-time** | SignalR 8.0.0, 1.1.0 |
| **Dependency Injection** | Microsoft.Extensions.DependencyInjection 8.0.0 |
| **MVVM** | CommunityToolkit.Mvvm 8.2.2 |
| **Database Migrations** | Entity Framework Core Tools 8.0.7 |
| **PDF/Export** | ClosedXML 0.102.2, QuestPDF 2024.3.3 |
| **Barcode** | ZXing.Net 0.16.9 |
| **AutoMapper** | AutoMapper 13.0.1 |
| **FluentValidation** | FluentValidation 11.9.0 |
| **Logging** | Serilog, Microsoft.Extensions.Logging |

### 1.3 Key Dependencies

**Core Project:**
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Logging.Abstractions`
- `System.ComponentModel.Annotations`
- `FluentValidation`
- `AutoMapper`
- `CommunityToolkit.Mvvm`

**Infrastructure Project:**
- `Microsoft.EntityFrameworkCore.Design` (8.0.7)
- `Npgsql.EntityFrameworkCore.PostgreSQL` (8.0.4)
- `BCrypt.Net-Next` (4.0.3)
- `Microsoft.AspNetCore.SignalR.Client`
- `ClosedXML`, `QuestPDF`
- `ZXing.Net` (Barcode generation)

**API Project:**
- `Microsoft.AspNetCore.SignalR` (1.1.0)
- `Swashbuckle.AspNetCore` (6.4.0) - Swagger/OpenAPI

---

## 2. Database Configuration & Model

### 2.1 Database Type
- **Primary Database**: PostgreSQL 8.0+
- **Connection String Format**: 
  ```
  Host=localhost;Port=5432;Database=TransitManager;Username=postgres;Password=XXXX;Include Error Detail=true
  ```
- **ORM**: Entity Framework Core 8.0.7 with async operations

### 2.2 Database Context

**File**: `TransitManager.Infrastructure/Data/TransitContext.cs`

**DbSets:**
```csharp
public DbSet<Client> Clients { get; set; }
public DbSet<Colis> Colis { get; set; }
public DbSet<Conteneur> Conteneurs { get; set; }
public DbSet<Paiement> Paiements { get; set; }
public DbSet<Utilisateur> Utilisateurs { get; set; }
public DbSet<Document> Documents { get; set; }
public DbSet<AuditLog> AuditLogs { get; set; }
public DbSet<Barcode> Barcodes { get; set; }
public DbSet<Vehicule> Vehicules { get; set; }
```

### 2.3 Key Database Features

**Soft Deletes with Query Filters:**
```csharp
modelBuilder.Entity<Client>().HasQueryFilter(e => e.Actif);
modelBuilder.Entity<Colis>().HasQueryFilter(e => e.Actif);
modelBuilder.Entity<Vehicule>().HasQueryFilter(e => e.Actif);
modelBuilder.Entity<Conteneur>().HasQueryFilter(e => e.Actif);
modelBuilder.Entity<Paiement>().HasQueryFilter(e => e.Actif);
```

**Automatic Audit Trail:**
- Records `DateCreation`, `DateModification`, `CreePar`, `ModifiePar`
- Full change tracking with JSON serialization of before/after values
- `AuditLog` table for compliance

**Decimal Precision:**
```csharp
// All decimal properties set to precision(18, 2)
foreach (var property in modelBuilder.Model.GetEntityTypes()
    .SelectMany(t => t.GetProperties())
    .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
{
    property.SetPrecision(18);
    property.SetScale(2);
}
```

**UTC DateTime Handling:**
- All dates automatically converted to UTC before saving
- Handles both Local and Unspecified DateTimeKind

---

## 3. Domain Model & Entities

### 3.1 Core Entities

#### **BaseEntity** (Abstract)
All entities inherit from this base class:
```csharp
public abstract class BaseEntity : INotifyPropertyChanged
{
    public Guid Id { get; set; }                    // Unique identifier
    public DateTime DateCreation { get; set; }
    public DateTime? DateModification { get; set; }
    public string? CreePar { get; set; }            // Created by
    public string? ModifiePar { get; set; }         // Modified by
    public bool Actif { get; set; } = true;        // Soft delete flag
    public byte[]? RowVersion { get; set; }        // Concurrency control
}
```

#### **Utilisateur** (User)
```csharp
public class Utilisateur : BaseEntity
{
    public string NomUtilisateur { get; set; }     // Username (unique)
    public string Nom { get; set; }
    public string Prenom { get; set; }
    public string Email { get; set; }
    public string MotDePasseHash { get; set; }     // BCrypt hash
    public string? PasswordSalt { get; set; }
    public RoleUtilisateur Role { get; set; }     // Admin, Gestionnaire, Operateur, Comptable, Invite
    public string? Telephone { get; set; }
    public string? PhotoProfil { get; set; }
    public DateTime? DerniereConnexion { get; set; }
    public int TentativesConnexionEchouees { get; set; }
    public DateTime? DateVerrouillage { get; set; } // Account lockout after 5 failures (30 min)
    public bool DoitChangerMotDePasse { get; set; }
    public string? TokenReinitialisation { get; set; }
    public DateTime? ExpirationToken { get; set; }
    public string? Preferences { get; set; }       // JSON
    public string? PermissionsSpecifiques { get; set; } // JSON
    public string Theme { get; set; } = "Clair";
    public string Langue { get; set; } = "fr-FR";
    public string FuseauHoraire { get; set; } = "Europe/Paris";
    public bool NotificationsActivees { get; set; } = true;
    public bool NotificationsEmail { get; set; } = true;
    public bool NotificationsSMS { get; set; } = false;
    public virtual ICollection<AuditLog> Audits { get; set; }
    
    // Methods
    public bool APermission(string permission) { /* Checks role & specific permissions */ }
}
```

**Roles with Default Permissions:**
- **Administrateur**: All permissions (*)
- **Gestionnaire**: clients.*, colis.*, conteneurs.*, paiements.voir, rapports.*, documents.*
- **Operateur**: clients.voir, clients.creer, colis.*, conteneurs.voir, documents.voir
- **Comptable**: clients.voir, paiements.*, factures.*, rapports.financiers, documents.financiers
- **Invite**: *.voir (read-only)

#### **Client** (Customer)
```csharp
public class Client : BaseEntity
{
    public string CodeClient { get; set; }         // Auto-generated: CLI-YYYYMMDD-XXXX
    public string Nom { get; set; }
    public string Prenom { get; set; }
    public string TelephonePrincipal { get; set; }
    public string? TelephoneSecondaire { get; set; }
    public string? Email { get; set; }
    public string? AdressePrincipale { get; set; }
    public string? AdresseLivraison { get; set; }
    public string? Ville { get; set; }
    public string? CodePostal { get; set; }
    public string? Pays { get; set; }
    public DateTime DateInscription { get; set; }
    public string? Commentaires { get; set; }
    public string? PieceIdentite { get; set; }
    public string? TypePieceIdentite { get; set; }
    public string? NumeroPieceIdentite { get; set; }
    public bool EstClientFidele { get; set; }
    public decimal PourcentageRemise { get; set; }
    public decimal Impayes { get; set; }
    public int NombreConteneursUniques { get; set; }
    public decimal VolumeTotalExpedié { get; set; }
    public int NombreTotalEnvois { get; set; }
    
    // Navigation
    public virtual ICollection<Colis> Colis { get; set; }
    public virtual ICollection<Vehicule> Vehicules { get; set; }
    public virtual ICollection<Paiement> Paiements { get; set; }
}
```

#### **Colis** (Package)
```csharp
public class Colis : BaseEntity
{
    public string NumeroReference { get; set; }    // Auto: REF-YYYYMMDD-XXXXX
    public Guid ClientId { get; set; }
    public Guid? ConteneurId { get; set; }
    public DateTime DateArrivee { get; set; }
    public EtatColis Etat { get; set; }           // BonEtat, Abime, Perdu
    public StatutColis Statut { get; set; }       // EnAttente, EnTransit, Livre
    public TypeColis Type { get; set; }
    public int NombrePieces { get; set; }
    public string Designation { get; set; }
    public decimal Volume { get; set; }
    public decimal ValeurDeclaree { get; set; }
    public bool EstFragile { get; set; }
    public bool ManipulationSpeciale { get; set; }
    public string? InstructionsSpeciales { get; set; }
    public string? Photos { get; set; }
    public DateTime? DateDernierScan { get; set; }
    public string? LocalisationActuelle { get; set; }
    public string? HistoriqueScan { get; set; }
    public DateTime? DateLivraison { get; set; }
    public string? Destinataire { get; set; }
    public string? TelephoneDestinataire { get; set; }
    public string? AdresseLivraison { get; set; }
    public string DestinationFinale { get; set; }
    public TypeEnvoi TypeEnvoi { get; set; }
    public bool LivraisonADomicile { get; set; }
    public decimal PrixTotal { get; set; }
    public decimal SommePayee { get; set; }
    public string? NumeroPlomb { get; set; }
    public string? InventaireJson { get; set; }    // Inventory details
    
    // Navigation
    public virtual Client? Client { get; set; }
    public virtual Conteneur? Conteneur { get; set; }
    public virtual ICollection<Barcode> Barcodes { get; set; }
    public virtual ICollection<Paiement> Paiements { get; set; }
    
    // Calculated
    public decimal RestantAPayer => PrixTotal - SommePayee;
    public bool EstEnRetard => Statut == StatutColis.EnAttente && (DateTime.UtcNow - DateArrivee).TotalDays > 5;
}
```

#### **Vehicule** (Vehicle/Car)
```csharp
public class Vehicule : BaseEntity
{
    public Guid ClientId { get; set; }
    public string Immatriculation { get; set; }    // License plate
    public string Marque { get; set; }             // Brand
    public string Modele { get; set; }             // Model
    public int Annee { get; set; }                 // Year
    public int Kilometrage { get; set; }
    public string DestinationFinale { get; set; }
    public decimal ValeurDeclaree { get; set; }
    public string? Destinataire { get; set; }
    public string? TelephoneDestinataire { get; set; }
    public TypeVehicule Type { get; set; }        // Car, Truck, Van, etc.
    public string? Commentaires { get; set; }
    public decimal PrixTotal { get; set; }
    public decimal SommePayee { get; set; }
    public StatutVehicule Statut { get; set; }    // EnAttente, EnTransit, Livre
    public Guid? ConteneurId { get; set; }
    public string? NumeroPlomb { get; set; }
    public string? EtatDesLieux { get; set; }
    public string? EtatDesLieuxRayures { get; set; }
    
    // Navigation
    public virtual Client? Client { get; set; }
    public virtual Conteneur? Conteneur { get; set; }
    public virtual ICollection<Paiement> Paiements { get; set; }
    
    // Calculated
    public decimal RestantAPayer => PrixTotal - SommePayee;
}
```

#### **Conteneur** (Container/Shipment Container)
```csharp
public class Conteneur : BaseEntity
{
    public string NumeroDossier { get; set; }     // Dossier number (unique)
    public string? NumeroPlomb { get; set; }      // Seal number
    public string? NomCompagnie { get; set; }     // Shipping company
    public string? NomTransitaire { get; set; }   // Customs broker
    public string Destination { get; set; }
    public string PaysDestination { get; set; }
    public StatutConteneur Statut { get; set; }  // Reçu, Chargé, Parti, Arrivé, Dédouané, Livré
    public DateTime? DateReception { get; set; }
    public DateTime? DateChargement { get; set; }
    public DateTime? DateDepart { get; set; }
    public DateTime? DateArriveeDestination { get; set; }
    public DateTime? DateDedouanement { get; set; }
    public DateTime? DateCloture { get; set; }
    public string? Commentaires { get; set; }
    
    // Navigation
    public virtual ICollection<Colis> Colis { get; set; }
    public virtual ICollection<Vehicule> Vehicules { get; set; }
    
    // Calculated
    public int NombreColis => Colis?.Count ?? 0;
    public int NombreVehicules => Vehicules?.Count ?? 0;
    public IEnumerable<Client> ClientsDistincts => (Colis + Vehicules clients)
}
```

#### **Paiement** (Payment)
```csharp
public class Paiement : BaseEntity
{
    public string NumeroRecu { get; set; }        // Auto: REC-YYYYMMDD-XXXX
    public Guid ClientId { get; set; }
    public Guid? ColisId { get; set; }
    public Guid? VehiculeId { get; set; }
    public Guid? ConteneurId { get; set; }
    public Guid? FactureId { get; set; }
    public DateTime DatePaiement { get; set; }
    public decimal Montant { get; set; }
    public string Devise { get; set; } = "EUR";
    public decimal TauxChange { get; set; } = 1;
    public TypePaiement ModePaiement { get; set; } // Espece, Cheque, Carte, Virement
    public string? Reference { get; set; }
    public string? Banque { get; set; }
    public StatutPaiement Statut { get; set; }    // Valide, Rejeté, Annulé
    public string? Description { get; set; }
    public string? Commentaires { get; set; }
    public string? RecuScanne { get; set; }
    public DateTime? DateEcheance { get; set; }
    public bool RappelEnvoye { get; set; }
    public DateTime? DateDernierRappel { get; set; }
    
    // Navigation
    public virtual Client? Client { get; set; }
    public virtual Colis? Colis { get; set; }
    public virtual Vehicule? Vehicule { get; set; }
    public virtual Conteneur? Conteneur { get; set; }
    
    // Calculated
    public decimal MontantLocal => Montant * TauxChange;
    public bool EstEnRetard => DateEcheance.HasValue && DateEcheance.Value < DateTime.UtcNow && Statut != StatutPaiement.Paye;
}
```

### 3.2 Supporting Entities

- **Barcode**: Links to Colis, contains barcode value
- **Document**: Files associated with entities
- **AuditLog**: Complete audit trail with user, action, before/after JSON
- **Notification**: System notifications
- **BackupModels**: Backup/restore functionality
- **InventaireItem**: Inventory tracking

### 3.3 Enums

```csharp
public enum RoleUtilisateur 
{ 
    Administrateur, Gestionnaire, Operateur, Comptable, Invite 
}

public enum StatutColis 
{ 
    EnAttente, EnTransit, Livre 
}

public enum EtatColis 
{ 
    BonEtat, Abime, Perdu 
}

public enum TypeColis 
{ 
    Colis, Palette, Fût, Baril, Envelope, etc. 
}

public enum TypeVehicule 
{ 
    Voiture, Camion, Moto, Van, Quad, etc. 
}

public enum TypeEnvoi 
{ 
    Standard, Express, Economique 
}

public enum StatutVehicule 
{ 
    EnAttente, EnTransit, Livre 
}

public enum StatutConteneur 
{ 
    Reçu, Chargé, Parti, Arrivé, Dédouané, Livré 
}

public enum StatutPaiement 
{ 
    Valide, Rejeté, Annulé, Paye 
}

public enum TypePaiement 
{ 
    Espece, Cheque, Carte, Virement 
}
```

---

## 4. API Implementation

### 4.1 API Startup Configuration

**File**: `TransitManager.API/Program.cs`

**Key Configuration:**

```csharp
// 1. PostgreSQL Database with DbContextFactory
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextFactory<TransitContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Service Registration
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IColisService, ColisService>();
builder.Services.AddScoped<IVehiculeService, VehiculeService>();
builder.Services.AddScoped<IConteneurService, ConteneurService>();
builder.Services.AddScoped<IPaiementService, PaiementService>();
// ... more services

// 3. Repository Pattern
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IColisRepository, ColisRepository>();

// 4. AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// 5. JSON Configuration with Reference Handling
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// 6. Swagger/OpenAPI
builder.Services.AddSwaggerGen();

// 7. SignalR for Real-time Notifications
builder.Services.AddSignalR();

// 8. CORS Configuration (Development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(origin => true)
              .AllowCredentials();
    });
});
```

### 4.2 API Controllers & Endpoints

#### **ClientsController**
```
GET    /api/clients                   - Get all active clients
GET    /api/clients/{id}              - Get specific client
POST   /api/clients                   - Create new client
PUT    /api/clients/{id}              - Update client
DELETE /api/clients/{id}              - Delete (soft) client
```

#### **ColisController**
```
GET    /api/colis                     - Get all packages
GET    /api/colis/{id}                - Get specific package
POST   /api/colis                     - Create new package
PUT    /api/colis/{id}                - Update package
DELETE /api/colis/{id}                - Delete package
PUT    /api/colis/inventaire          - Update inventory details
```

#### **VehiculesController**
```
GET    /api/vehicules                 - Get all vehicles
GET    /api/vehicules/{id}            - Get specific vehicle
POST   /api/vehicules                 - Create new vehicle
PUT    /api/vehicules/{id}            - Update vehicle
```

#### **ConteneursController**
```
GET    /api/conteneurs                - Get all containers
GET    /api/conteneurs/{id}           - Get specific container
POST   /api/conteneurs                - Create new container
PUT    /api/conteneurs/{id}           - Update container
DELETE /api/conteneurs/{id}           - Delete container
```

#### **PaiementsController**
```
GET    /api/paiements/{id}            - Get specific payment
GET    /api/paiements/colis/{colisId}     - Get payments for package
GET    /api/paiements/vehicule/{vehiculeId} - Get payments for vehicle
POST   /api/paiements                 - Create payment
PUT    /api/paiements/{id}            - Update payment
DELETE /api/paiements/{id}            - Delete payment
```

#### **UtilitiesController**
```
GET    /api/utilities/generate-barcode - Generate barcode
[Additional utility endpoints]
```

### 4.3 SignalR Hub

**File**: `TransitManager.API/Hubs/NotificationHub.cs`

```csharp
public class NotificationHub : Hub
{
    public async Task ClientUpdated(Guid clientId)
    {
        await Clients.Others.SendAsync("ClientUpdated", clientId);
    }
    
    // Additional hub methods for other entities
}
```

**Hub Endpoint**: `/notificationHub`

---

## 5. Authentication & Authorization

### 5.1 Current Authentication Mechanism

**Implementation**: `TransitManager.Infrastructure/Services/AuthenticationService.cs`

**Features:**
- Username/password authentication
- BCrypt password hashing (4.0.3)
- Account lockout after 5 failed attempts (30 minutes)
- Password strength validation:
  - Minimum 8 characters
  - Requires uppercase, lowercase, digit, special character
- Audit logging of login/logout
- Password reset token system
- Session tracking with last login date

**Code:**
```csharp
public async Task<AuthenticationResult> LoginAsync(string username, string password)
{
    var user = await context.Utilisateurs
        .FirstOrDefaultAsync(u => u.NomUtilisateur == username && u.Actif);
    
    if (user?.EstVerrouille)
        return new AuthenticationResult { Success = false, ErrorMessage = "Account locked" };
    
    if (!BCryptNet.Verify(password, user.MotDePasseHash))
    {
        user.TentativesConnexionEchouees++;
        if (user.TentativesConnexionEchouees >= 5)
            user.DateVerrouillage = DateTime.UtcNow.AddMinutes(30);
        return new AuthenticationResult { Success = false };
    }
    
    user.TentativesConnexionEchouees = 0;
    user.DerniereConnexion = DateTime.UtcNow;
    await context.SaveChangesAsync();
    
    return new AuthenticationResult { Success = true, User = user };
}
```

### 5.2 Authorization System

**Permission Model**: Role-based with dynamic permissions

**Utilisateur Methods:**
```csharp
public bool APermission(string permission)
{
    // Admins have all permissions
    if (Role == RoleUtilisateur.Administrateur)
        return true;
    
    // Check role-based permissions
    var permissionsRole = GetPermissionsParRole(Role);
    if (permissionsRole.Contains(permission))
        return true;
    
    // Check specific permissions (JSON stored)
    // TODO: Deserialize and check specific permissions
    return false;
}
```

**Default Role Permissions:**
- Administrateur: `*` (all)
- Gestionnaire: `clients.*, colis.*, conteneurs.*, paiements.voir, rapports.*, documents.*`
- Operateur: `clients.voir, clients.creer, colis.*, conteneurs.voir, documents.voir`
- Comptable: `clients.voir, paiements.*, factures.*, rapports.financiers, documents.financiers`
- Invite: `*.voir` (read-only)

### 5.3 Current API Security Issues

**⚠️ CRITICAL GAPS:**
- No authentication middleware in API (no [Authorize] attributes)
- No JWT/Bearer token authentication
- No API token validation
- All endpoints are publicly accessible
- AuthenticationService is used only in client applications (WPF/Mobile), not in API

**For Web Version, Must Implement:**
1. JWT token generation upon login
2. Authentication middleware
3. Authorization middleware with role/permission checks
4. Refresh token mechanism
5. Token expiration and renewal

---

## 6. Data Access Layer (Repository Pattern)

### 6.1 Generic Repository

**File**: `TransitManager.Infrastructure/Repositories/GenericRepository.cs`

```csharp
public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
    Task<T> UpdateAsync(T entity);
    Task<bool> RemoveAsync(T entity);              // Soft delete
    Task<bool> RemoveRangeAsync(IEnumerable<T> entities);
    Task<int> CountAsync();
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    IQueryable<T> Query();
    IQueryable<T> QueryNoTracking();
}
```

**Features:**
- Soft delete (sets Actif = false)
- Automatic query filtering (only returns Actif = true)
- Async all the way
- No-track queries for read-only operations

### 6.2 Specialized Repositories

**IClientRepository**
```csharp
Task<Client?> GetByCodeAsync(string codeClient);
Task<Client?> GetWithDetailsAsync(Guid id);
Task<IEnumerable<Client>> GetActiveClientsAsync();
Task<IEnumerable<Client>> SearchAsync(string searchTerm);
Task<IEnumerable<Client>> GetFideleClientsAsync();
Task<IEnumerable<Client>> GetClientsWithUnpaidBalanceAsync();
Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId = null);
Task<bool> IsPhoneUniqueAsync(string phone, Guid? excludeId = null);
```

**IColisRepository**
- Custom queries for package status, container associations
- Barcode queries
- Inventory-related operations

**IConteneurRepository**
- Container status queries
- Package/vehicle aggregation
- Sealing/customs operations

---

## 7. Service Layer

### 7.1 Service Architecture

**Pattern**: Service interface + implementation

**Key Services:**
- `IAuthenticationService` - User authentication
- `IClientService` - Client management
- `IColisService` - Package management
- `IVehiculeService` - Vehicle management
- `IConteneurService` - Container management
- `IPaiementService` - Payment management
- `INotificationService` - System notifications
- `INotificationHubService` - SignalR notification publishing
- `IBarcodeService` - Barcode generation
- `IExportService` - Data export (PDF, Excel)
- `IBackupService` - Database backup/restore
- `IPrintingService` - Document printing

### 7.2 Example: ClientService

```csharp
public class ClientService : IClientService
{
    private readonly IDbContextFactory<TransitContext> _contextFactory;
    private readonly INotificationService _notificationService;
    private readonly INotificationHubService _notificationHubService;
    
    public async Task<Client?> GetByIdAsync(Guid id) { ... }
    public async Task<IEnumerable<Client>> GetActiveClientsAsync() { ... }
    public async Task<IEnumerable<Client>> SearchAsync(string searchTerm) { ... }
    public async Task<Client> CreateAsync(Client client) { ... }
    public async Task UpdateAsync(Client client) { ... }
    public async Task<bool> DeleteAsync(Guid id) { ... }
}
```

### 7.3 Dependency Injection

**Pattern**: Constructor injection with DbContextFactory
```csharp
public class ClientService : IClientService
{
    public ClientService(IDbContextFactory<TransitContext> contextFactory, 
                       INotificationService notificationService,
                       INotificationHubService notificationHubService)
    {
        _contextFactory = contextFactory;
        _notificationService = notificationService;
        _notificationHubService = notificationHubService;
    }
}
```

---

## 8. WPF Desktop Application

### 8.1 Architecture Overview

**Framework**: WPF (.NET 8.0-windows)

**Key Projects:**
- `ViewModels/` - MVVM ViewModels
- `Views/` - XAML Views organized by feature
- `Models/` - Client-side models
- `Services/` - Local services (navigation, dialog, SignalR)
- `Helpers/` - Utility classes
- `Controls/` - Custom WPF controls
- `Converters/` - Value converters
- `Resources/` - Images, styles, themes

### 8.2 View Organization

```
Views/
├── Auth/
│   └── LoginWindow.xaml
├── Clients/
│   ├── ClientsListView.xaml
│   └── ClientDetailView.xaml
├── Colis/
├── Vehicules/
├── Conteneurs/
├── Paiements/
├── Finance/
├── Dashboard/
├── Notifications/
└── Inventaire/
```

### 8.3 Key Technologies

- **MVVM**: CommunityToolkit.Mvvm 8.2.2
- **Styling**: MahApps.Metro, MaterialDesignThemes
- **Charts**: LiveChartsCore.SkiaSharpView.WPF
- **Real-time**: SignalR client
- **Logging**: Serilog
- **DI**: Microsoft.Extensions.DependencyInjection

### 8.4 Navigation Service

**File**: `WPF/Helpers/NavigationService.cs`
- Page-based navigation
- ViewModel binding
- Dialog services

### 8.5 SignalR Integration

**File**: `WPF/Helpers/SignalRClientService.cs`
- Real-time client updates
- Notification handling
- Connection management

---

## 9. Mobile Application (.NET MAUI)

### 9.1 Architecture Overview

**Framework**: .NET MAUI (net8.0-android)

**Target**: Android platform

**Project Structure:**
- `Views/` - XAML pages
- `ViewModels/` - MVVM ViewModels
- `Services/` - API client, local services
- `Models/` - Domain models
- `Converters/` - Value converters
- `Resources/` - Images, fonts, styles
- `Platforms/` - Platform-specific code (Android, iOS, etc.)

### 9.2 API Client Integration

**File**: `Mobile/Services/ITransitApi.cs`

Uses **Refit 7.0.0** for declarative HTTP client:

```csharp
[Get("/api/clients")]
Task<IEnumerable<Client>> GetClientsAsync();

[Get("/api/clients/{id}")]
Task<Client> GetClientByIdAsync(Guid id);

[Post("/api/clients")]
Task<Client> CreateClientAsync([Body] Client client);

[Get("/api/colis")]
Task<IEnumerable<ColisListItemDto>> GetColisAsync();

[Get("/api/conteneurs/{id}")]
Task<Conteneur> GetConteneurByIdAsync(Guid id);

[Get("/api/paiements/vehicule/{vehiculeId}")]
Task<IEnumerable<Paiement>> GetPaiementsForVehiculeAsync(Guid vehiculeId);
```

### 9.3 Pages

```
Views/
├── ClientsPage.xaml
├── ClientDetailPage.xaml
├── AddEditClientPage.xaml
├── ColisPage.xaml
├── ColisDetailPage.xaml
├── AddEditColisPage.xaml
├── VehiculesPage.xaml
├── VehiculeDetailPage.xaml
├── AddEditVehiculePage.xaml
├── EtatDesLieuxPage.xaml
├── EditEtatDesLieuxPage.xaml
├── PaiementVehiculePage.xaml
└── AddEditPaiementPage.xaml
```

### 9.4 Key Technologies

- **MVVM**: CommunityToolkit.Mvvm 8.2.2
- **HTTP Client**: Refit 7.0.0
- **Real-time**: SignalR Client 8.0.0
- **UI Framework**: .NET MAUI Controls

---

## 10. DTOs (Data Transfer Objects)

### 10.1 DTO Classes

Located in: `TransitManager.Core/DTOs/`

**List DTOs** (optimized for list views):
- `ClientListItemDto` - Lightweight client data
- `ColisListItemDto` - Package list with key info
- `VehiculeListItemDto` - Vehicle summary

**Create/Update DTOs** (for API requests):
- `CreateColisDto` - Package creation
- `UpdateColisDto` - Package updates
- `CreateClientDto` - Client creation
- `UpdateInventaireDto` - Inventory updates

**Benefits:**
- Prevents over-fetching of data
- Hides internal implementation
- Controls API response shape
- Optimizes performance

---

## 11. Configuration & Settings

### 11.1 Configuration Files

**API Configuration**: `appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=TransitManager;..."
  },
  "Logging": { "LogLevel": { "Default": "Information" } },
  "AllowedHosts": "*"
}
```

**WPF Configuration**: `appsettings.json`
```json
{
  "ConnectionStrings": { ... },
  "AppSettings": {
    "ApplicationName": "Transit Manager",
    "Version": "1.0.0",
    "DefaultLanguage": "fr-FR",
    "EnableAutoBackup": true,
    "MaxLoginAttempts": 5,
    "LockoutDurationMinutes": 30
  },
  "FileStorage": { ... },
  "SignalRSettings": { "HubUrl": "https://localhost:5001/transitHub" },
  "SecuritySettings": { ... },
  "FeatureFlags": { ... }
}
```

### 11.2 Environment-Specific Configuration

- `appsettings.Development.json` - Development overrides
- `appsettings.override.json` - Local overrides (not in git)

---

## 12. Key Architectural Patterns

### 12.1 Repository Pattern
- Generic base repository with common operations
- Specialized repositories for complex queries
- Abstraction of data access logic

### 12.2 Service Layer Pattern
- Business logic separated from data access
- Reusable across API and desktop/mobile clients
- Dependency injection throughout

### 12.3 MVVM Pattern (WPF & Mobile)
- Clear separation of concerns
- ViewModel-first navigation
- Data binding for reactive UI

### 12.4 Soft Deletes
- Logical deletion instead of hard deletion
- Query filters automatically exclude deleted items
- Preserved audit trails

### 12.5 Audit Logging
- Automatic change tracking
- Before/after JSON values
- User and timestamp tracking
- Compliance-ready

### 12.6 Unit of Work Pattern (DbContext)
- Single context per operation
- DbContextFactory for async operations
- Automatic transaction management

---

## 13. Recommendations for Web Version

### 13.1 Authentication Enhancement

**MUST IMPLEMENT:**
1. JWT token generation (RS256 or HS256)
2. Token refresh mechanism
3. Authentication middleware
4. Authorization middleware with role/permission checking
5. CORS configuration for web origin
6. HTTPS enforcement

**Suggested Implementation:**
```csharp
// Add to API Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.Authority = "https://your-auth-server";
        options.Audience = "api";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = "your-issuer",
            ValidateAudience = true,
            ValidAudience = "api",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Then in controllers:
[Authorize]
public class ClientsController { ... }

[Authorize(Roles = "Administrateur,Gestionnaire")]
[HttpPost]
public async Task<ActionResult<Client>> CreateClient([FromBody] Client client) { ... }
```

### 13.2 Role-Based Access Control (RBAC)

**Current**: Permission strings in database
**Recommended**: Implement granular role-based authorization

```csharp
// Authorization policies
builder.Services.AddAuthorization(options => {
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Administrateur"));
    
    options.AddPolicy("ManageClients", policy => 
        policy.RequireClaim("permission", "clients.create", "clients.edit"));
    
    options.AddPolicy("ViewFinance", policy => 
        policy.RequireRole("Comptable", "Administrateur"));
});

// In controller
[Authorize(Policy = "AdminOnly")]
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteClient(Guid id) { ... }
```

### 13.3 Web Technologies to Add

**Frontend Frameworks to Consider:**
- React, Vue.js, or Angular
- Material-UI or Bootstrap for styling
- Redux/Context for state management
- SignalR JavaScript client for real-time updates

**Backend Enhancements:**
- API versioning (v1, v2)
- Rate limiting
- Request/response logging middleware
- Health check endpoint
- OpenAPI/Swagger auto-documentation

### 13.4 Data Structure Changes Needed

**For multi-tenant or user isolation:**
- Consider adding `TenantId` to entities
- Add user-level filtering in queries
- Document access control rules per entity

**For web-specific features:**
- Store user preferences in database
- Client-side session tokens
- User activity tracking for analytics

### 13.5 API Response Standardization

**Recommended Standard Response**:
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### 13.6 Pagination & Filtering

**Implement for web lists:**
```csharp
[HttpGet]
public async Task<ApiResponse<PagedResult<ClientListItemDto>>> GetClients(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? search = null,
    [FromQuery] string? sortBy = null,
    [FromQuery] bool sortDesc = false)
{
    // Implement pagination with IQueryable
}
```

---

## 14. Database Seeding

**Default Admin User:**
```
Username: admin
Email: admin@transitmanager.com
Password: Admin@123
Role: Administrateur
```

Seeded in `TransitContext.OnModelCreating()` with BCrypt hash.

---

## 15. Error Handling & Logging

### 15.1 Logging Configuration

**Serilog**: File-based daily rolling logs
```json
"Serilog": {
  "MinimumLevel": "Information",
  "WriteTo": [
    {
      "Name": "File",
      "Args": {
        "path": "Logs/transitmanager-.log",
        "rollingInterval": "Day",
        "retainedFileCountLimit": 30
      }
    }
  ]
}
```

### 15.2 Exception Handling

**Recommended Global Exception Handler:**
```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;
        
        var response = new ApiResponse<object>
        {
            Success = false,
            Message = "Internal server error",
            StatusCode = 500,
            Timestamp = DateTime.UtcNow
        };
        
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(response);
    });
});
```

---

## 16. Database Migrations

**Using Entity Framework Core CLI:**

```bash
# Add migration
dotnet ef migrations add AddNewFeature -p TransitManager.Infrastructure -s TransitManager.API

# Update database
dotnet ef database update -p TransitManager.Infrastructure -s TransitManager.API

# View pending migrations
dotnet ef migrations list
```

**Migrations Location**: `TransitManager.Infrastructure/Migrations/`

---

## 17. Performance Considerations

### 17.1 Database Optimization
- Use `.AsNoTracking()` for read-only queries
- Use `.AsSplitQuery()` for multiple eager loads
- Implement pagination for large result sets
- Use indexes on frequently queried columns (ClientId, Status, etc.)

### 17.2 Caching
- Consider implementing distributed caching for reference data
- Cache role/permission definitions
- Cache frequently accessed client data (with TTL)

### 17.3 Query Optimization
- Avoid N+1 query problems with proper eager loading
- Use projection to fetch only needed columns
- Implement query result limiting

---

## 18. Security Best Practices

### 18.1 Implemented
- BCrypt password hashing
- Account lockout mechanism
- Password strength validation
- Audit logging
- Soft deletes (data preservation)

### 18.2 To Implement for Web
- HTTPS enforcement (HSTS headers)
- CORS origin validation
- Rate limiting per IP/user
- Input validation (FluentValidation ready)
- SQL injection prevention (EF Core)
- XSS protection (validate all inputs)
- CSRF token validation

### 18.3 Database Security
- Never use default/weak credentials
- Use parameterized queries (EF Core default)
- Implement column encryption for PII
- Regular backups with encryption

---

## Summary Table: Readiness for Web Version

| Component | Status | Notes |
|-----------|--------|-------|
| **Core API** | Ready | REST API fully functional |
| **Database** | Ready | PostgreSQL properly configured |
| **Authentication** | ⚠️ Partial | Username/password only, no JWT |
| **Authorization** | ⚠️ Partial | Role-based, needs middleware |
| **Data Models** | Ready | Comprehensive domain model |
| **Services** | Ready | Complete business logic layer |
| **Repositories** | Ready | Generic + specialized repositories |
| **Logging** | Ready | Serilog configured |
| **Validation** | ⚠️ Partial | FluentValidation available, not yet integrated |
| **API Documentation** | Ready | Swagger/OpenAPI configured |
| **CORS** | Ready | Configured for all origins (dev) |
| **SignalR** | Ready | Real-time notification hub |
| **Error Handling** | ⚠️ Partial | No global exception handler |
| **Pagination** | ⚠️ Partial | Foundation in repository, not in controllers |

---

## File Structure Reference

```
src/
├── TransitManager.API/
│   ├── Program.cs                          [Main configuration]
│   ├── appsettings.json
│   ├── Controllers/
│   │   ├── ClientsController.cs
│   │   ├── ColisController.cs
│   │   ├── VehiculesController.cs
│   │   ├── ConteneursController.cs
│   │   ├── PaiementsController.cs
│   │   └── UtilitiesController.cs
│   ├── Hubs/
│   │   └── NotificationHub.cs              [SignalR hub]
│   └── TransitManager.API.csproj
│
├── TransitManager.Core/
│   ├── Entities/
│   │   ├── BaseEntity.cs                   [Base class for all entities]
│   │   ├── Utilisateur.cs                  [User model]
│   │   ├── Client.cs
│   │   ├── Colis.cs
│   │   ├── Vehicule.cs
│   │   ├── Conteneur.cs
│   │   ├── Paiement.cs
│   │   ├── Barcode.cs
│   │   ├── Document.cs
│   │   ├── AuditLog.cs
│   │   └── ... [more entities]
│   ├── DTOs/
│   │   ├── ClientListItemDto.cs
│   │   ├── ColisListItemDto.cs
│   │   ├── CreateColisDto.cs
│   │   └── ... [more DTOs]
│   ├── Enums/
│   │   ├── RoleUtilisateur.cs
│   │   ├── StatutColis.cs
│   │   ├── EtatColis.cs
│   │   └── ... [more enums]
│   ├── Interfaces/
│   │   ├── IAuthenticationService.cs
│   │   ├── IClientService.cs
│   │   ├── IColisService.cs
│   │   └── ... [more interfaces]
│   └── TransitManager.Core.csproj
│
├── TransitManager.Infrastructure/
│   ├── Data/
│   │   ├── TransitContext.cs               [DbContext]
│   │   ├── TransitContextFactory.cs
│   │   └── Configurations/                 [EF mappings]
│   ├── Repositories/
│   │   ├── GenericRepository.cs            [Base repository]
│   │   ├── ClientRepository.cs
│   │   ├── ColisRepository.cs
│   │   └── ... [more repositories]
│   ├── Services/
│   │   ├── AuthenticationService.cs
│   │   ├── ClientService.cs
│   │   ├── ColisService.cs
│   │   ├── NotificationService.cs
│   │   ├── NotificationHubService.cs
│   │   └── ... [more services]
│   ├── Migrations/                         [EF migrations]
│   └── TransitManager.Infrastructure.csproj
│
├── TransitManager.WPF/
│   ├── ViewModels/                         [MVVM ViewModels]
│   ├── Views/                              [XAML Views]
│   ├── Models/
│   ├── Services/
│   │   ├── NavigationService.cs
│   │   ├── DialogService.cs
│   │   └── SignalRClientService.cs
│   ├── Helpers/
│   ├── Controls/
│   ├── Converters/
│   ├── Resources/
│   ├── App.xaml
│   ├── appsettings.json
│   └── TransitManager.WPF.csproj
│
└── TransitManager.Mobile/
    ├── Views/                              [MAUI Pages]
    ├── ViewModels/                         [MVVM ViewModels]
    ├── Services/
    │   └── ITransitApi.cs                  [Refit API client]
    ├── Models/
    ├── Converters/
    ├── Resources/
    ├── Platforms/                          [Platform-specific code]
    ├── MauiProgram.cs
    └── TransitManager.Mobile.csproj
```

---

**Document Version**: 1.0
**Last Updated**: November 3, 2025
**Status**: Ready for Web Version Planning

