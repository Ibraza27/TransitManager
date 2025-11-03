# TransitManager - Architecture Summary for Web Version Planning

## Quick Reference Guide

### Current Architecture Status

**Maturity Level**: Production-Ready API + Desktop/Mobile Clients (WPF & MAUI)
**Critical Gap for Web**: No JWT/Bearer authentication or authorization middleware

---

## 1. Technology Stack at a Glance

| Component | Technology | Version |
|-----------|-----------|---------|
| **Framework** | .NET 8.0 | 8.0 LTS |
| **Web API** | ASP.NET Core | 8.0 |
| **Database** | PostgreSQL | 8.0+ |
| **ORM** | Entity Framework Core | 8.0.7 |
| **Desktop** | WPF | net8.0-windows |
| **Mobile** | .NET MAUI | net8.0-android |
| **Authentication** | BCrypt | 4.0.3 |
| **API Documentation** | Swagger/OpenAPI | 6.4.0 |
| **Real-time** | SignalR | 8.0.0 |

---

## 2. Project Layout

```
TransitManager.sln
├── TransitManager.API              (REST API - MUST ADD AUTH)
├── TransitManager.Core             (Entities, DTOs, Interfaces)
├── TransitManager.Infrastructure   (Repositories, Services, DbContext)
├── TransitManager.WPF              (Desktop UI - MVVM + SignalR)
└── TransitManager.Mobile           (Android UI - MAUI + Refit)
```

---

## 3. Core Domain Entities

| Entity | Purpose | Key Features |
|--------|---------|--------------|
| **Utilisateur** | User accounts | 5 roles, BCrypt hashing, account lockout |
| **Client** | Customers | Auto-code generation, soft delete |
| **Colis** | Packages | Status tracking, barcode support, inventory |
| **Vehicule** | Vehicles for shipment | Type-based (Car, Truck, Van, Moto), status tracking |
| **Conteneur** | Shipping containers | Multi-stage (Reçu→Chargé→Parti→Arrivé→Dédouané) |
| **Paiement** | Payments | Multi-currency, status tracking, due dates |

### Role Hierarchy

1. **Administrateur** - Full system access
2. **Gestionnaire** - Manage clients, packages, containers, view payments
3. **Operateur** - Create/view clients, manage packages
4. **Comptable** - View clients, manage payments and invoices
5. **Invite** - Read-only access

---

## 4. API Endpoint Structure

### Base URL
`https://your-api-domain/api`

### Main Controllers (Unsecured)

```
Clients:
  GET    /api/clients
  GET    /api/clients/{id}
  POST   /api/clients
  PUT    /api/clients/{id}
  DELETE /api/clients/{id}

Colis (Packages):
  GET    /api/colis
  POST   /api/colis
  PUT    /api/colis/{id}
  DELETE /api/colis/{id}
  PUT    /api/colis/inventaire

Vehicules:
  GET    /api/vehicules
  POST   /api/vehicules
  PUT    /api/vehicules/{id}

Conteneurs (Containers):
  GET    /api/conteneurs
  POST   /api/conteneurs
  PUT    /api/conteneurs/{id}
  DELETE /api/conteneurs/{id}

Paiements (Payments):
  GET    /api/paiements/{id}
  GET    /api/paiements/colis/{colisId}
  GET    /api/paiements/vehicule/{vehiculeId}
  POST   /api/paiements
  PUT    /api/paiements/{id}
  DELETE /api/paiements/{id}

SignalR Hub:
  WebSocket /notificationHub
```

---

## 5. Authentication & Authorization Status

### Current Implementation
- **Method**: Username/password validation against database
- **Password Hashing**: BCrypt (salted)
- **Account Security**: Lockout after 5 failed attempts (30 min)
- **Where Used**: WPF & Mobile clients only
- **API Status**: NO authentication middleware - all endpoints public

### Critical Issue
❌ **API endpoints are completely unsecured**
- No JWT tokens
- No [Authorize] attributes on controllers
- No permission checking in API
- All endpoints accessible to anyone

### Required for Web Version
✓ JWT token generation
✓ Bearer token validation middleware
✓ [Authorize] attributes on controllers
✓ Role-based authorization policies
✓ Refresh token mechanism
✓ Token expiration and renewal

---

## 6. Database Configuration

### Connection Details
- **Database**: PostgreSQL
- **Default Credentials**: 
  - Host: localhost
  - Port: 5432
  - Username: postgres
  - Database: TransitManager
  - (Password in appsettings.json)

### Key Features
- **Soft Deletes**: Actif flag, query filters prevent access to deleted records
- **Audit Trail**: Automatic DateCreation, DateModification, CreePar, ModifiePar
- **Change Logging**: Full before/after values in AuditLog table
- **UTC Dates**: Automatic conversion to UTC before saving
- **Decimal Precision**: All monetary values as decimal(18,2)

### Sample Admin User (Seeded)
```
Username: admin
Email: admin@transitmanager.com
Password: Admin@123 (BCrypt hashed)
Role: Administrateur
```

---

## 7. Service Architecture

### Service Layer Pattern
Each entity has a corresponding service:
- `IClientService` → `ClientService`
- `IColisService` → `ColisService`
- `IVehiculeService` → `VehiculeService`
- `IConteneurService` → `ConteneurService`
- `IPaiementService` → `PaiementService`

### Dependency Injection
All services use constructor injection with DbContextFactory:
```csharp
public ClientService(IDbContextFactory<TransitContext> contextFactory, ...)
{
    _contextFactory = contextFactory;
}
```

### Repository Pattern
Generic + Specialized repositories with soft delete support

---

## 8. Data Access Layer

### Generic Repository Features
- `GetByIdAsync(Guid id)`
- `GetAllAsync()` → Only active records
- `FindAsync(Expression)` → Query with filters
- `AddAsync`, `UpdateAsync`, `RemoveAsync` (soft delete)
- `GetPagedAsync(pageNumber, pageSize)`
- `QueryNoTracking()` for read-only operations

### Soft Delete Implementation
```csharp
// RemoveAsync sets Actif = false instead of hard delete
public virtual async Task<bool> RemoveAsync(T entity)
{
    entity.Actif = false;
    _dbSet.Update(entity);
    await _context.SaveChangesAsync();
    return true;
}

// Query filters automatically exclude inactive records
modelBuilder.Entity<Client>().HasQueryFilter(e => e.Actif);
```

---

## 9. Desktop Client (WPF)

### Architecture
- **Pattern**: MVVM with CommunityToolkit.Mvvm
- **Real-time**: SignalR client for notifications
- **HTTP**: Uses Infrastructure services directly
- **Navigation**: Custom NavigationService with ViewModel binding
- **Styling**: MahApps.Metro + MaterialDesignThemes
- **Charts**: LiveChartsCore.SkiaSharpView.WPF

### View Organization
```
Views/
├── Auth/          (LoginWindow)
├── Clients/       (List, Detail, Edit)
├── Colis/         (Package views)
├── Vehicules/     (Vehicle views)
├── Conteneurs/    (Container views)
├── Paiements/     (Payment views)
└── Dashboard/     (Analytics)
```

---

## 10. Mobile Client (.NET MAUI)

### Architecture
- **Framework**: .NET MAUI (Android)
- **HTTP Client**: Refit 7.0 (declarative)
- **Pattern**: MVVM with CommunityToolkit.Mvvm
- **Real-time**: SignalR Client
- **Target**: net8.0-android

### API Integration Example
```csharp
public interface ITransitApi
{
    [Get("/api/clients")]
    Task<IEnumerable<Client>> GetClientsAsync();
    
    [Post("/api/clients")]
    Task<Client> CreateClientAsync([Body] Client client);
    
    [Get("/api/paiements/vehicule/{vehiculeId}")]
    Task<IEnumerable<Paiement>> GetPaiementsForVehiculeAsync(Guid vehiculeId);
}
```

---

## 11. Key Files for Web Version Planning

### Must-Review Files
1. **Program.cs** → `/src/TransitManager.API/Program.cs`
   - Current configuration, DI setup, CORS settings

2. **Controllers** → `/src/TransitManager.API/Controllers/`
   - ClientsController.cs, ColisController.cs, etc.
   - Currently have NO [Authorize] attributes

3. **AuthenticationService** → `/src/TransitManager.Infrastructure/Services/AuthenticationService.cs`
   - Password hashing and validation logic
   - Model for implementing JWT

4. **TransitContext** → `/src/TransitManager.Infrastructure/Data/TransitContext.cs`
   - Database configuration, soft delete filters
   - Default admin user seeding

5. **Utilisateur Entity** → `/src/TransitManager.Core/Entities/Utilisateur.cs`
   - Role enum and APermission() method
   - Permission definition logic

---

## 12. Configuration Files

### API Settings
**File**: `/src/TransitManager.API/appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=TransitManager;..."
  },
  "Logging": { "LogLevel": { "Default": "Information" } },
  "AllowedHosts": "*"
}
```

### Application Settings (WPF)
**File**: `/src/TransitManager.WPF/appsettings.json`
- Complete configuration for security, features, storage paths
- Email settings, barcode settings, export settings
- Feature flags for controlling functionality

---

## 13. Implementation Checklist for Web Version

### Priority 1: Security (MUST HAVE)
- [ ] Add JWT authentication middleware to API
- [ ] Add [Authorize] attributes to all sensitive endpoints
- [ ] Implement role-based authorization policies
- [ ] Add refresh token mechanism
- [ ] Implement token expiration validation
- [ ] Add CORS for web domain only (not AllowAll)

### Priority 2: API Improvements
- [ ] Add global exception handler middleware
- [ ] Implement pagination for all list endpoints
- [ ] Add request/response logging middleware
- [ ] Add rate limiting per IP/user
- [ ] Implement health check endpoint (/health)
- [ ] Add API versioning strategy

### Priority 3: Frontend
- [ ] Choose framework (React, Vue, Angular)
- [ ] Implement JWT token storage (localStorage/sessionStorage)
- [ ] Add interceptor for Authorization header
- [ ] Implement login/logout UI
- [ ] Add role-based UI rendering
- [ ] Integrate SignalR client for real-time updates

### Priority 4: Data & Compliance
- [ ] Review existing audit logging
- [ ] Add user-level data filtering (if multi-tenant)
- [ ] Implement activity logging for web users
- [ ] Add HTTPS enforcement (HSTS headers)
- [ ] Review password policies for compliance

---

## 14. Performance & Scalability Considerations

### Current Optimizations
- ✓ Async/await throughout
- ✓ DbContextFactory for concurrent operations
- ✓ Query filters reduce database load
- ✓ NoTracking queries for read-only operations

### Recommended Additions
- [ ] Redis caching for reference data
- [ ] Database query optimization (indexes)
- [ ] Connection pooling tuning
- [ ] API response compression
- [ ] Client-side caching strategies
- [ ] Pagination enforcement (max 1000 items)

---

## 15. Deployment Considerations

### Current Production Setup
- PostgreSQL database (local or cloud)
- ASP.NET Core hosting (IIS, Docker, Linux)
- SignalR WebSocket support required
- HTTPS certificate required

### Web Deployment Options
1. **Monolithic**: Single .NET 8 app hosting API + static SPA
2. **Separate**: API on separate domain, SPA on CDN
3. **Containerized**: Docker containers + orchestration
4. **Serverless**: Not recommended for SignalR real-time features

---

## 16. Testing Strategy Recommendations

### Unit Testing
- Test service layer business logic
- Test repository queries
- Test validation rules

### Integration Testing
- Test API endpoints with authentication
- Test database operations
- Test SignalR connections

### Security Testing
- SQL injection tests
- XSS vulnerability tests
- CSRF token validation
- JWT token attacks

---

## 17. Migration Path from WPF/Mobile to Web

### Code Reuse
- ✓ Reuse all Service layer code
- ✓ Reuse all Repository layer code
- ✓ Reuse all Entity models
- ✓ Reuse all DTOs
- ✓ Reuse all business logic

### Changes Needed
- ✗ Complete new UI layer (React/Vue/Angular)
- ✗ Authentication middleware for API
- ✗ Authorization policies
- ✗ Response formatting/standardization
- ✗ API versioning strategy

---

## 18. Database Migrations

### How to Apply Migrations
```bash
# Navigate to solution root
cd /home/user/TransitManager

# Apply migrations
dotnet ef database update \
  -p src/TransitManager.Infrastructure \
  -s src/TransitManager.API

# Add new migration
dotnet ef migrations add FeatureName \
  -p src/TransitManager.Infrastructure \
  -s src/TransitManager.API
```

### Migration Files Location
`/src/TransitManager.Infrastructure/Migrations/`

---

## 19. API Response Format Recommendation

### Standardized Response Structure
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

// Usage
return Ok(new ApiResponse<Client> 
{ 
    Success = true, 
    Data = client, 
    StatusCode = 200 
});
```

---

## 20. Key Metrics & Monitoring

### Suggested Monitoring
- [ ] API response times
- [ ] Database query performance
- [ ] Authentication failure rates
- [ ] SignalR connection health
- [ ] Audit log growth rate
- [ ] User activity patterns

---

## Summary: Readiness Assessment

| Area | Status | Gap |
|------|--------|-----|
| **API Framework** | ✓ Production Ready | None |
| **Database** | ✓ Well Designed | None |
| **Business Logic** | ✓ Complete | None |
| **Data Models** | ✓ Comprehensive | Minor (DTOs) |
| **Authentication** | ⚠️ Partial | No JWT |
| **Authorization** | ⚠️ Partial | No middleware |
| **API Security** | ❌ Missing | Critical |
| **Error Handling** | ⚠️ Partial | Global handler |
| **Documentation** | ✓ Swagger Ready | None |

---

## Quick Start for Web Development

### Step 1: Add JWT Authentication
Modify `/src/TransitManager.API/Program.cs`:
```csharp
var jwtSecret = builder.Configuration["Jwt:Secret"];
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* config */ });
builder.Services.AddAuthorization();
```

### Step 2: Secure Endpoints
Add to controllers:
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // Require authentication for all
public class ClientsController : ControllerBase
{
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [HttpPost]
    public async Task<ActionResult<Client>> CreateClient(...)
```

### Step 3: Create Login Endpoint
Add authentication controller to issue JWT tokens

### Step 4: Build Web Frontend
Choose framework, integrate API client, implement auth flow

---

**Document Version**: 1.0
**Last Updated**: November 3, 2025
**Location**: `/home/user/TransitManager/ARCHITECTURE_SUMMARY.md`
**Full Details**: See `TransitManager_Architecture_Overview.md` (1386 lines)

