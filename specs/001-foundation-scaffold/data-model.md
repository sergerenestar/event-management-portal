# Data Model: Sprint 0 — Foundation Scaffold

**Scope**: Sprint 0 introduces one entity: `AdminUser`. All other entities defined in spec 03
are deferred to their respective feature sprints. The initial EF Core migration creates only
the `AdminUsers` table.

---

## Entity: AdminUser

Represents an authenticated admin user of the portal.

**Module owner**: Auth
**File**: `backend/src/EventPortal.Api/Modules/Auth/Entities/AdminUser.cs`

### Schema

```
AdminUsers
├── Id                  int           PK, identity, auto-increment
├── Email               nvarchar(256) NOT NULL, unique index
├── DisplayName         nvarchar(256) NOT NULL
├── IdentityProvider    nvarchar(64)  NOT NULL  -- 'microsoft' | 'google'
├── ExternalObjectId    nvarchar(256) NOT NULL  -- Entra External ID object ID
├── IsActive            bit           NOT NULL, default 1
├── CreatedAt           datetime2     NOT NULL
└── LastLoginAt         datetime2     NULL
```

### EF Core Entity Class

```csharp
// Modules/Auth/Entities/AdminUser.cs
namespace EventPortal.Api.Modules.Auth.Entities;

public class AdminUser
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string IdentityProvider { get; set; } = string.Empty;
    public string ExternalObjectId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
```

### EF Core Configuration

```csharp
// Modules/Shared/Persistence/AppDbContext.cs — entity registration
public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
```

```csharp
// In AppDbContext.OnModelCreating or IEntityTypeConfiguration<AdminUser>
builder.Entity<AdminUser>(e =>
{
    e.ToTable("AdminUsers");
    e.HasKey(x => x.Id);
    e.Property(x => x.Email).HasMaxLength(256).IsRequired();
    e.HasIndex(x => x.Email).IsUnique();
    e.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
    e.Property(x => x.IdentityProvider).HasMaxLength(64).IsRequired();
    e.Property(x => x.ExternalObjectId).HasMaxLength(256).IsRequired();
    e.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
    e.Property(x => x.LastLoginAt).HasColumnType("datetime2");
});
```

---

## Base Entity Convention

All future entities will extend a `BaseEntity` class to enforce the `Id`, `CreatedAt`,
`UpdatedAt` convention defined in spec 03.

**File**: `backend/src/EventPortal.Api/Modules/Shared/Persistence/BaseEntity.cs`

```csharp
namespace EventPortal.Api.Modules.Shared.Persistence;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

`AdminUser` does not extend `BaseEntity` because it has `LastLoginAt` instead of `UpdatedAt`
(it is not a mutable record in the standard sense).

---

## Initial Migration

**Migration name**: `InitialCreate`
**Output directory**: `backend/src/EventPortal.Api/Modules/Auth/Migrations/`

**Creates**:
- Table: `AdminUsers` — as defined above
- Unique index on `AdminUsers.Email`

**Does not create**:
- Any other table from spec 03 (those belong to future sprints)

**Command to generate**:
```bash
cd backend
dotnet ef migrations add InitialCreate \
  --project src/EventPortal.Api \
  --startup-project src/EventPortal.Api \
  --output-dir Modules/Auth/Migrations
```

**Command to apply (local)**:
```bash
dotnet ef database update \
  --project src/EventPortal.Api \
  --startup-project src/EventPortal.Api
```

---

## Deferred Entities (future sprints)

| Entity | Sprint | Module |
|---|---|---|
| RefreshToken | Sprint 1 | Auth |
| Event, TicketType | Sprint 2 | Events |
| Registration, DailyRegistrationSnapshot | Sprint 2 | Registrations |
| SmsCampaign, SmsRecipientSegment | Sprint 3 | Campaigns |
| SocialPostDraft, SocialPostApproval, PublishedPost | Sprint 4 | SocialPosts |
| SessionIngestion, SessionTranscript, SessionSummary, SessionQuote | Sprint 5 | Sessions |
| PdfReport | Sprint 6 | Reports |
| AuditLog, BackgroundJobStatus | Sprint 1+ | AuditLogs / Shared |
| IntegrationConnections | Sprint 2+ | Shared |
