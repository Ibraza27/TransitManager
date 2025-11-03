# TransitManager Architecture Documentation

This folder contains comprehensive documentation of the TransitManager system architecture designed to support planning and implementing a new web version with role-based access control.

## Documents Overview

### 1. **ARCHITECTURE_SUMMARY.md** (START HERE)
**Quick Reference Guide** - Best for quick lookup and getting oriented
- Technology stack overview
- Project layout and structure
- Core domain entities
- API endpoint structure  
- Authentication & authorization status
- Database configuration
- Service architecture
- Implementation checklist for web version
- Readiness assessment table

**Best for**: Quick understanding, meetings, planning

---

### 2. **TransitManager_Architecture_Overview.md** (COMPREHENSIVE)
**Complete Technical Reference** (1,386 lines)
- Detailed explanation of all components
- Complete code examples
- Database migrations guide
- Performance considerations
- Security best practices
- Deployment strategies
- Testing recommendations
- Migration path analysis
- File structure reference

**Best for**: Developers, architects, deep technical review

---

### 3. **ARCHITECTURE_DIAGRAM.txt** (VISUAL)
**System Architecture Diagrams**
- Complete system architecture flowchart
- Data flow diagrams
- Entity relationships
- Authentication flow (current vs. needed)
- Security gaps visualization
- Implementation roadmap (5 phases)

**Best for**: Visual learners, presentations, whiteboarding

---

### 4. **README_ARCHITECTURE.md** (THIS FILE)
Navigation guide for all architecture documentation

---

## Quick Facts

| Aspect | Details |
|--------|---------|
| **Framework** | .NET 8.0 LTS |
| **API** | ASP.NET Core 8.0 REST API |
| **Database** | PostgreSQL 8.0+ |
| **Desktop** | WPF (.NET 8.0-windows) |
| **Mobile** | .NET MAUI (Android) |
| **Current State** | Production-ready backend |
| **Critical Gap** | No JWT/Bearer authentication |

---

## Key Findings

### Strengths
✓ **Complete Backend**: REST API with all business logic
✓ **Well-Designed Database**: PostgreSQL with soft deletes, audit logging
✓ **Comprehensive Models**: 10+ entities with proper relationships
✓ **Authentication Mechanism**: BCrypt hashing, account lockout
✓ **Role-Based System**: 5 roles with permission framework
✓ **Real-time Support**: SignalR hub for notifications
✓ **Clean Architecture**: Service layer, repository pattern
✓ **Working Clients**: Desktop (WPF) and Mobile (MAUI) fully functional

### Critical Gaps for Web Version
❌ **No API Authentication Middleware** - All endpoints are public
❌ **No JWT/Bearer Tokens** - No way to identify users in API
❌ **No Authorization Middleware** - No [Authorize] attributes on controllers
❌ **No Permission Enforcement** - Permission check exists but isn't used
❌ **No Rate Limiting** - No protection against brute force
❌ **No Global Exception Handler** - Inconsistent error responses
❌ **Unrestricted CORS** - Allows all origins (development only)

---

## What's Currently Working

### API Endpoints (Unsecured)
```
GET    /api/clients
POST   /api/clients
GET    /api/colis
POST   /api/colis
GET    /api/vehicules
POST   /api/vehicules
GET    /api/conteneurs
POST   /api/conteneurs
GET    /api/paiements
POST   /api/paiements
WebSocket /notificationHub (SignalR)
```

### Services Layer
- 12+ business logic services
- Dependency injection throughout
- Async/await patterns
- Event publishing for notifications

### Data Access
- Generic repository pattern
- Specialized repositories for complex queries
- Soft delete support
- Automatic audit trail
- UTC date handling

### Authentication (Client-Side Only)
- Username/password validation
- BCrypt hashing
- 5 failed attempts lockout (30 min)
- Password strength validation
- Session tracking

---

## For Web Version Implementation

### Immediate Actions (Priority 1)
1. Add JWT authentication middleware
2. Add [Authorize] attributes to controllers
3. Create login endpoint that issues JWT tokens
4. Implement refresh token mechanism
5. Add authorization policies for roles
6. Restrict CORS to web domain

### Architecture Decisions Needed
1. **Frontend Framework**: React, Vue, or Angular?
2. **Deployment Model**: Monolithic or separate API/SPA?
3. **Token Storage**: localStorage or sessionStorage?
4. **Multi-tenancy**: Support multiple organizations?
5. **Caching Strategy**: Redis or in-memory?

### Database Decisions
1. Keep single PostgreSQL instance or shard by tenant?
2. Add multi-tenancy support or single organization?
3. Implement user-level data filtering?
4. Archive strategy for old data?

---

## File Locations Reference

### API Project
- `/src/TransitManager.API/Program.cs` - Configuration & DI setup
- `/src/TransitManager.API/Controllers/` - API endpoints (need auth)
- `/src/TransitManager.API/Hubs/NotificationHub.cs` - SignalR

### Core Project
- `/src/TransitManager.Core/Entities/` - Domain models
- `/src/TransitManager.Core/Enums/` - Enumerations
- `/src/TransitManager.Core/DTOs/` - Data transfer objects
- `/src/TransitManager.Core/Interfaces/` - Service contracts

### Infrastructure Project
- `/src/TransitManager.Infrastructure/Data/TransitContext.cs` - Database context
- `/src/TransitManager.Infrastructure/Services/` - Business logic
- `/src/TransitManager.Infrastructure/Repositories/` - Data access
- `/src/TransitManager.Infrastructure/Migrations/` - Database versions

### Configuration
- `/src/TransitManager.API/appsettings.json` - API settings
- `/src/TransitManager.WPF/appsettings.json` - Comprehensive app settings
- `/Directory.Packages.props` - Centralized package versions

---

## Default Credentials (Development)

```
Username: admin
Email: admin@transitmanager.com
Password: Admin@123
Role: Administrateur
```

**WARNING**: Change immediately in production!

---

## Connection String

```
Host=localhost;Port=5432;Database=TransitManager;Username=postgres;Password=XXXX
```

Configure in `/src/TransitManager.API/appsettings.json`

---

## How to Use This Documentation

### For Project Managers
1. Read ARCHITECTURE_SUMMARY.md section 20 "Readiness Assessment"
2. Review section 13 "Implementation Checklist"
3. Check ARCHITECTURE_DIAGRAM.txt for "Implementation Roadmap"

### For Architects
1. Read full TransitManager_Architecture_Overview.md
2. Review ARCHITECTURE_DIAGRAM.txt for system flows
3. Focus on sections 3-6 (Database, Models, API, Authentication)

### For Backend Developers
1. Start with ARCHITECTURE_SUMMARY.md sections 1-8
2. Deep dive into TransitManager_Architecture_Overview.md sections 2-7
3. Review security gaps section before starting

### For Frontend Developers
1. Read ARCHITECTURE_SUMMARY.md sections 4, 5, 11
2. Review authentication flow in ARCHITECTURE_DIAGRAM.txt
3. Check TransitManager_Architecture_Overview.md section 13 (Implementation Checklist)

### For DevOps/Deployment
1. Review TransitManager_Architecture_Overview.md section 15 (Deployment)
2. Check database migration guide (section 18)
3. Review configuration requirements

---

## Next Steps

1. **Review**: Read ARCHITECTURE_SUMMARY.md completely
2. **Assess**: Check current codebase against findings
3. **Plan**: Create implementation plan for JWT authentication
4. **Design**: Choose frontend framework and architecture
5. **Secure**: Implement authentication middleware first
6. **Test**: Security testing of API endpoints
7. **Build**: Develop web frontend
8. **Deploy**: Setup production environment

---

## Important Notes

- All documentation is current as of November 3, 2025
- Codebase uses .NET 8.0 (Long-Term Support)
- Database is PostgreSQL 8.0+
- API is production-ready for internal use only
- No backward compatibility concerns for web version
- All existing code can be reused (Services, Repositories, Models)

---

## Support & Questions

When reviewing code:
1. Check `/src/TransitManager.Core/Entities/` for model definitions
2. Check `/src/TransitManager.Infrastructure/Services/` for business logic
3. Check `/src/TransitManager.API/Controllers/` for API contracts
4. Check `/src/TransitManager.Infrastructure/Data/TransitContext.cs` for database setup

---

**Document Location**: `/home/user/TransitManager/README_ARCHITECTURE.md`
**Last Updated**: November 3, 2025
**Status**: Complete & Ready for Web Development Planning

