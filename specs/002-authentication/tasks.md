# Tasks: Sprint 1 — Authentication and Admin Access

**Input**: `specs/002-authentication/spec.md`, `README_event_management_portal.md`, Sprint 0 scaffold
**Prerequisites**: Sprint 0 ✅ · `dotnet build` ✅ · `npm run build` ✅ · `GET /health` ✅
**Note**: Do NOT use ASP.NET Core Identity. Entra External ID is the identity provider. The portal issues its own lightweight JWT only.

---

## User Story Map

| Story | Deliverable | Done When |
|---|---|---|
| US1 | `POST /api/auth/login` — Entra token validation + AdminUser upsert + JWT issuance | Returns signed access token (15 min) + HttpOnly refresh cookie |
| US2 | `POST /api/auth/refresh` — refresh token rotation | New access token issued, old refresh token invalidated |
| US3 | `GET /api/auth/me` + `POST /api/auth/logout` | Me returns admin profile; logout revokes refresh token server-side |
| US4 | RefreshTokens table | EF Core entity + migration, tokens stored hashed |
| US5 | React login page + MSAL integration | Microsoft and Google sign-in buttons work end-to-end |
| US6 | Silent session restore on page load | App calls `/refresh` on load if no access token in store |
| US7 | Route protection + top bar update | Unauthenticated → `/login`; top bar shows admin name + Sign Out |

---

## Phase 1: Backend — New Entities and Migration

**Purpose**: Add the `RefreshTokens` table and confirm `AdminUsers` has all required fields before any service code is written.

- [ ] T001 Verify `Modules/Auth/Entities/AdminUser.cs` has all fields:
  - `Id` (int, PK), `Email` (nvarchar 256), `DisplayName` (nvarchar 256)
  - `IdentityProvider` (nvarchar 64), `ExternalObjectId` (nvarchar 256)
  - `IsActive` (bool, default true), `CreatedAt` (datetime2), `LastLoginAt` (datetime2, nullable)
  - If any fields are missing — add them now before generating the migration
- [ ] T002 Create `backend/src/EventPortal.Api/Modules/Auth/Entities/RefreshToken.cs`:
  ```
  Id               int, identity, PK
  AdminUserId      int, FK → AdminUsers.Id
  TokenHash        nvarchar(512) — SHA-256 hash of raw token, never raw value
  ExpiresAt        datetime2, UTC
  IsRevoked        bool, default false
  CreatedAt        datetime2, UTC
  AdminUser        navigation property
  ```
- [ ] T003 Register `RefreshToken` in `AppDbContext`:
  - Add `public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();`
  - `OnModelCreating`: unique index on `TokenHash`; index on `AdminUserId`; cascade delete from `AdminUsers`
- [ ] T004 Generate EF Core migration:
  ```bash
  cd backend
  dotnet ef migrations add AddRefreshTokens \
    --project src/EventPortal.Api \
    --startup-project src/EventPortal.Api \
    --output-dir Modules/Auth/Migrations
  ```
  Verify migration creates `RefreshTokens` table with FK to `AdminUsers`
- [ ] T005 Run migration locally:
  ```bash
  dotnet ef database update \
    --project src/EventPortal.Api \
    --startup-project src/EventPortal.Api
  ```

**Checkpoint**: `dotnet build backend/` — 0 errors. `RefreshTokens` table visible in local DB.

---

## Phase 2: Backend — Repositories

**Purpose**: Build the data access layer for AdminUsers and RefreshTokens.

- [ ] T006 Create `Modules/Auth/Repositories/IAdminUserRepository.cs`:
  ```csharp
  Task<AdminUser?> GetByExternalObjectIdAsync(string externalObjectId);
  Task<AdminUser?> GetByIdAsync(int id);
  Task<AdminUser> CreateAsync(AdminUser user);
  Task UpdateLastLoginAsync(int userId);
  ```
- [ ] T007 [P] Create `Modules/Auth/Repositories/AdminUserRepository.cs`:
  - `GetByExternalObjectIdAsync` — query by `ExternalObjectId`
  - `GetByIdAsync` — query by PK
  - `CreateAsync` — sets `CreatedAt = DateTime.UtcNow`, `IsActive = true`, calls `SaveChangesAsync`
  - `UpdateLastLoginAsync` — sets `LastLoginAt = DateTime.UtcNow`, calls `SaveChangesAsync`
- [ ] T008 [P] Create `Modules/Auth/Repositories/IRefreshTokenRepository.cs`:
  ```csharp
  Task<RefreshToken?> GetByHashAsync(string tokenHash);
  Task CreateAsync(RefreshToken token);
  Task RevokeAsync(int tokenId);
  Task RevokeAllForUserAsync(int adminUserId);
  ```
- [ ] T009 [P] Create `Modules/Auth/Repositories/RefreshTokenRepository.cs`:
  - `GetByHashAsync` — query by `TokenHash` where `IsRevoked = false` and `ExpiresAt > DateTime.UtcNow`
  - `CreateAsync` — sets `CreatedAt = DateTime.UtcNow`, calls `SaveChangesAsync`
  - `RevokeAsync` — sets `IsRevoked = true` by token ID, calls `SaveChangesAsync`
  - `RevokeAllForUserAsync` — bulk revoke all active tokens for a user (used on logout)
- [ ] T010 Register repositories in `Program.cs`:
  - `builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>()`
  - `builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>()`

**Checkpoint**: `dotnet build backend/` — 0 errors.

---

## Phase 3: Backend — Token Service

**Purpose**: Implement JWT generation and refresh token hashing as isolated, testable services.

- [ ] T011 Create `Modules/Auth/Services/ITokenService.cs`:
  ```csharp
  string GenerateAccessToken(AdminUser user);
  string GenerateRefreshToken();
  string HashToken(string rawToken);
  ClaimsPrincipal? ValidateAccessToken(string token);
  ```
- [ ] T012 Implement `Modules/Auth/Services/TokenService.cs`:
  - `GenerateAccessToken`: claims `sub` (userId), `email`, `name`, `role: Admin`; expiry from `Jwt:ExpiryMinutes` (default 15); sign with `HmacSha256` using `Jwt:SigningKey`
  - `GenerateRefreshToken`: `RandomNumberGenerator.GetBytes(64)` → Base64 string (raw — caller hashes before persisting)
  - `HashToken`: SHA-256 → hex string; same input always returns same hash
  - `ValidateAccessToken`: validate signature + issuer + audience + expiry; return `ClaimsPrincipal` or null
- [ ] T013 [P] Update `Modules/Shared/Security/JwtConfiguration.cs`:
  - Keep only middleware registration (`AddAuthentication().AddJwtBearer()`)
  - Remove any token generation logic from Sprint 0 stub if present
- [ ] T014 Register `TokenService` in `Program.cs`:
  - `builder.Services.AddScoped<ITokenService, TokenService>()`

**Checkpoint**: `dotnet build backend/` — 0 errors.

---

## Phase 4: Backend — Auth Service

**Purpose**: Core auth business logic — Entra validation, user upsert, token issuance, refresh, logout.

- [ ] T015 Add NuGet packages to `EventPortal.Api.csproj`:
  - `Microsoft.Identity.Web`
  - `Microsoft.IdentityModel.Tokens`
  - `System.IdentityModel.Tokens.Jwt`
- [ ] T016 Create `Modules/Auth/Dtos/LoginRequestDto.cs`:
  - `string EntraIdToken`, `string Provider` (`"Microsoft"` or `"Google"`)
- [ ] T017 [P] Create `Modules/Auth/Dtos/LoginResponseDto.cs`:
  - `string AccessToken`, `int ExpiresIn` (always 900), `AdminUserDto Admin`
- [ ] T018 [P] Create `Modules/Auth/Dtos/AdminUserDto.cs`:
  - `int Id`, `string Email`, `string DisplayName`, `bool IsActive`
- [ ] T019 [P] Create `Modules/Auth/Dtos/RefreshResponseDto.cs`:
  - `string AccessToken`, `int ExpiresIn`
- [ ] T020 Update `Modules/Auth/Services/IAuthService.cs`:
  ```csharp
  Task<(LoginResponseDto result, string rawRefreshToken)> LoginAsync(LoginRequestDto request);
  Task<(RefreshResponseDto result, string rawRefreshToken)> RefreshAsync(string rawRefreshToken);
  Task<AdminUserDto?> GetMeAsync(int userId);
  Task LogoutAsync(string rawRefreshToken);
  ```
- [ ] T021 Implement `Modules/Auth/Services/AuthService.cs`:
  - **LoginAsync**:
    1. Validate `EntraIdToken` using `Microsoft.Identity.Web` against configured tenant
    2. Extract claims: `oid`/`sub` (external object ID), `email`, `name`
    3. Upsert: `GetByExternalObjectIdAsync` → if found `UpdateLastLoginAsync`; if not found `CreateAsync`
    4. If `IsActive = false` → throw `UnauthorizedAccessException("Account is disabled")`
    5. Generate access token + raw refresh token via `ITokenService`
    6. Hash refresh token, persist `RefreshToken` entity with `ExpiresAt = UtcNow + 7 days`
    7. Write audit log: event `"Login"`, adminId, provider
    8. Return `LoginResponseDto` + raw refresh token
  - **RefreshAsync**:
    1. Hash incoming token via `ITokenService.HashToken`
    2. `GetByHashAsync` — if null → throw `UnauthorizedAccessException`
    3. `RevokeAsync` on old token (immediate rotation)
    4. Generate new access token + new raw refresh token
    5. Hash and persist new `RefreshToken` entity
    6. Write audit log: event `"TokenRefresh"`, adminId
    7. Return `RefreshResponseDto` + new raw refresh token
  - **GetMeAsync**: `GetByIdAsync` → map to `AdminUserDto`; if `IsActive = false` return null
  - **LogoutAsync**: hash token → `GetByHashAsync` → `RevokeAllForUserAsync` → audit log `"Logout"`
- [ ] T022 [P] Create `Modules/Auth/Validators/LoginRequestValidator.cs`:
  - `RuleFor(x => x.EntraIdToken).NotEmpty()`
  - `RuleFor(x => x.Provider).Must(p => p == "Microsoft" || p == "Google")`
- [ ] T023 Register in `Program.cs`:
  - `builder.Services.AddScoped<IAuthService, AuthService>()`
  - `builder.Services.AddScoped<IValidator<LoginRequestDto>, LoginRequestValidator>()`

**Checkpoint**: `dotnet build backend/` — 0 errors.

---

## Phase 5: Backend — Auth Controller

**Purpose**: Expose the 4 auth endpoints. Handle HttpOnly cookie for refresh token.

- [ ] T024 Implement `Modules/Auth/Controllers/AuthController.cs` — `[Route("api/v1/auth")]`, `[ApiController]`:

  - **`POST /login`** `[AllowAnonymous]`:
    - Call `AuthService.LoginAsync(request)`
    - Set HttpOnly cookie `ep_refresh`: `HttpOnly=true`, `Secure=true`, `SameSite=Strict`, `Expires=UtcNow+7days`, `Path="/api/v1/auth"`
    - Return `200 LoginResponseDto` (no refresh token in body)

  - **`POST /refresh`** `[AllowAnonymous]`:
    - Read `ep_refresh` from `Request.Cookies` — if missing return `401`
    - Call `AuthService.RefreshAsync(rawToken)`
    - Set new `ep_refresh` HttpOnly cookie (same options)
    - Return `200 RefreshResponseDto`

  - **`GET /me`** `[Authorize(Policy = "AdminOnly")]`:
    - Extract userId from JWT claim `sub`
    - Call `AuthService.GetMeAsync(userId)` — if null return `404`; if `IsActive=false` return `403`
    - Return `200 AdminUserDto`

  - **`POST /logout`** `[Authorize]`:
    - Read `ep_refresh` cookie — if present call `AuthService.LogoutAsync(rawToken)`
    - Delete `ep_refresh` cookie via `Response.Cookies.Delete`
    - Return `200 { message = "Logged out" }`

**Checkpoint**: `dotnet build backend/` — 0 errors. `GET /swagger` shows all 4 auth endpoints.

---

## Phase 6: Backend — CORS, Swagger, and appsettings

- [ ] T025 Add CORS policy to `Program.cs`:
  - Policy: `AllowFrontend`; `WithOrigins(config["Cors:AllowedOrigins"])`; `AllowAnyHeader()`; `AllowAnyMethod()`; **`AllowCredentials()`** — required for HttpOnly cookies
  - `app.UseCors("AllowFrontend")` before `UseAuthentication`
- [ ] T026 [P] Update Swagger in `Program.cs`: add `AddSecurityDefinition("Bearer")` + `AddSecurityRequirement`
- [ ] T027 [P] Update `appsettings.json`:
  - `"Cors": { "AllowedOrigins": "" }`
  - `"Entra": { "TenantId": "", "ClientId": "", "Audience": "" }`
  - `"Jwt": { "SigningKey": "", "Issuer": "EventPortal", "Audience": "EventPortalClient", "ExpiryMinutes": 15 }`
- [ ] T028 [P] Update `appsettings.Development.json.example`:
  - `Cors:AllowedOrigins` = `"http://localhost:5173"`
  - `Entra:TenantId`, `Entra:ClientId`, `Entra:Audience` = placeholder strings
  - `Jwt:SigningKey` = `"dev-signing-key-min-32-chars-replace-this"`
  - `Jwt:ExpiryMinutes` = `15`

**Checkpoint**: CORS not blocking requests from `http://localhost:5173`. Swagger padlock visible.

---

## Phase 7: Backend — Unit Tests

- [ ] T029 Add `Moq` and `FluentAssertions` to `EventPortal.Tests.csproj`
- [ ] T030 Delete `PlaceholderTest.cs` from Sprint 0
- [ ] T031 [P] Create `tests/EventPortal.Tests/Auth/AuthServiceTests.cs`:
  - Login new user → `CreateAsync` called → JWT returned → refresh token persisted
  - Login returning user → `UpdateLastLoginAsync` called → no duplicate
  - Login disabled user → `UnauthorizedAccessException`
  - Refresh valid token → old token revoked → new tokens returned
  - Refresh revoked token → `UnauthorizedAccessException`
  - Logout → `RevokeAllForUserAsync` called → audit log written
- [ ] T032 [P] Create `tests/EventPortal.Tests/Auth/TokenServiceTests.cs`:
  - `GenerateAccessToken` → non-empty, contains expected claims
  - `GenerateRefreshToken` → non-empty, two calls produce different values
  - `HashToken` → deterministic; different inputs produce different hashes
  - `ValidateAccessToken` → valid token returns principal; tampered token returns null; expired returns null

**Checkpoint**: `dotnet test backend/` → all tests pass, 0 failures.

---

## Phase 8: Frontend — MSAL Setup and Auth Service

- [ ] T033 Install inside `frontend/`: `npm install @azure/msal-browser @azure/msal-react`
- [ ] T034 Create `frontend/src/features/auth/msalConfig.js` — `msalConfig`, `loginRequest`, `googleLoginHint` (with `domain_hint: "google.com"`)
- [ ] T035 [P] Update `frontend/.env.example`: add `VITE_ENTRA_CLIENT_ID=`, `VITE_ENTRA_TENANT_ID=`, `VITE_REDIRECT_URI=http://localhost:5173`
- [ ] T036 Update `frontend/src/app/providers/AppProviders.jsx`: wrap in `<MsalProvider instance={msalInstance}>`
- [ ] T037 Implement `frontend/src/services/authService.js`:
  - `loginWithMicrosoft()` → `msalInstance.loginPopup(loginRequest)` → returns `idToken`
  - `loginWithGoogle()` → `msalInstance.loginPopup(googleLoginHint)` → returns `idToken`
  - `exchangeEntraToken(idToken, provider)` → `POST /api/v1/auth/login` → returns `LoginResponseDto`
  - `refreshSession()` → `POST /api/v1/auth/refresh` with `credentials: "include"` → returns `RefreshResponseDto`
  - `getMe()` → `GET /api/v1/auth/me` → returns `AdminUserDto`
  - `logout()` → `POST /api/v1/auth/logout` with `credentials: "include"` → clears store → `msalInstance.logoutPopup()`

**Checkpoint**: `npm run build` passes — no import errors.

---

## Phase 9: Frontend — Zustand Store Update

- [ ] T038 Rewrite `frontend/src/app/store/useAppStore.js`:
  - Shape: `accessToken: null`, `admin: null`, `isLoading: false`, `authError: null`
  - `setSession(token, admin)` — **in-memory only, no localStorage, no sessionStorage**
  - `clearSession()` — nulls `accessToken` and `admin`
  - `setLoading(bool)`, `setAuthError(msg)`
  - **Critical**: refresh token is HttpOnly cookie — never touched by JS store

**Checkpoint**: `npm run build` passes.

---

## Phase 10: Frontend — Login Page

- [ ] T039 Implement `frontend/src/features/auth/LoginPage.jsx`:
  - MUI centered layout: portal name "Event Portal", subtitle "Admin Access Only"
  - "Sign in with Microsoft" button → `loginWithMicrosoft()` → `exchangeEntraToken(token, "Microsoft")` → `setSession()` → navigate `/dashboard`
  - "Sign in with Google" button → same flow with `"Google"`
  - `LoadingSpinner` while in progress; `ErrorAlert` on failure
- [ ] T040 [P] Create `frontend/src/features/auth/useAuth.js`:
  - `isAuthenticated`, `admin`, `logout()` (calls `authService.logout()` + `clearSession()` + navigate `/login`)

**Checkpoint**: `http://localhost:5173/login` renders with both buttons, no console errors.

---

## Phase 11: Frontend — Session Restore and Route Guard

- [ ] T041 Rewrite `frontend/src/features/auth/AuthGuard.jsx`:
  - Mount: if `accessToken` in store → render children
  - Else: call `authService.refreshSession()` with `credentials: "include"`
    - Success → `setSession()` → render children
    - Failure → `clearSession()` → navigate `/login`
  - While checking → `<LoadingSpinner />`
- [ ] T042 [P] Update `frontend/src/services/apiClient.js`:
  - Request interceptor: inject `Authorization: Bearer <token>` from store
  - Response interceptor: on `401` → `clearSession()` → navigate `/login`
  - All calls use `withCredentials: true`
- [ ] T043 Update `frontend/src/app/router/AppRouter.jsx`:
  - All routes except `/login` wrapped in `<AuthGuard>`
  - `/` → redirect to `/dashboard` if authenticated, else `/login`
  - `/login` → redirect to `/dashboard` if already authenticated

**Checkpoint**: Unauthenticated → `/dashboard` redirects to `/login`. After login, page refresh restores session silently.

---

## Phase 12: Frontend — Top Bar Update

- [ ] T044 Update `frontend/src/components/layout/TopBar.jsx`:
  - Show `admin.displayName` in right side of `AppBar`
  - "Sign Out" button → `useAuth().logout()`
  - If `admin` is null → render nothing on right side

**Checkpoint**: Top bar shows display name and Sign Out after login. Sign Out clears session and redirects to `/login`.

---

## Phase 13: Docker and CI/CD Updates

- [ ] T045 Update `docker-compose.yml` API env: `Entra__TenantId`, `Entra__ClientId`, `Jwt__SigningKey`, `Cors__AllowedOrigins`
- [ ] T046 [P] Update `.env.example` at repo root: `ENTRA_TENANT_ID=`, `ENTRA_CLIENT_ID=`, `JWT_SIGNING_KEY=`
- [ ] T047 [P] Update `.github/workflows/dev-deploy.yml`: add Entra + JWT secrets to backend env; add `VITE_ENTRA_CLIENT_ID` + `VITE_ENTRA_TENANT_ID` to frontend build env
- [ ] T048 [P] Mirror same additions in `.github/workflows/prod-deploy.yml`

**Checkpoint**: `docker compose up --build` with `.env` file starts cleanly.

---

## Phase 14: Sprint 1 Validation

- [ ] T049 Full checklist:
  - [ ] `dotnet build backend/EventPortal.sln` → 0 errors, 0 warnings
  - [ ] `dotnet test backend/EventPortal.sln` → all tests pass
  - [ ] `npm run build` → 0 errors
  - [ ] `npm run lint` → 0 errors
  - [ ] `GET /swagger` → 4 auth endpoints with padlock
  - [ ] `GET /api/v1/auth/me` no token → `401`
  - [ ] `POST /api/v1/auth/login` invalid token → `400` or `401`
  - [ ] `/login` → renders with Microsoft + Google buttons
  - [ ] `/dashboard` unauthenticated → redirects to `/login`
  - [ ] Full login flow → dashboard loads, top bar shows display name
  - [ ] `AdminUsers` row created on first login
  - [ ] `AdminUsers.LastLoginAt` updated on second login
  - [ ] `RefreshTokens` row has hashed value (not raw token)
  - [ ] Page refresh → session restored silently
  - [ ] Sign Out → `/login`, refresh token row revoked in DB
  - [ ] Reusing revoked refresh token → `401`
  - [ ] Audit log entries for login, refresh, and logout
- [ ] T050 [P] Update `README.md`: `Sprint 1 — Authentication: ✅ Complete`
- [ ] T051 [P] Commit all work on branch `002-authentication`

---

## Dependencies and Execution Order

| Phase | Depends On |
|---|---|
| Phase 1 (Entities + Migration) | Sprint 0 |
| Phase 2 (Repositories) | Phase 1 |
| Phase 3 (Token Service) | Phase 1 |
| Phase 4 (Auth Service) | Phase 2 + Phase 3 |
| Phase 5 (Controller) | Phase 4 |
| Phase 6 (CORS + Swagger) | Phase 5 |
| Phase 7 (Tests) | Phase 4 |
| Phase 8 (MSAL + authService) | Phase 6 |
| Phase 9 (Zustand Store) | Independent |
| Phase 10 (Login Page) | Phase 8 + Phase 9 |
| Phase 11 (AuthGuard + Routing) | Phase 8 + Phase 9 |
| Phase 12 (Top Bar) | Phase 10 |
| Phase 13 (Docker + CI/CD) | All phases |
| Phase 14 (Validation) | All phases |

### Parallel Opportunities

| Developer A — Backend | Developer B — Frontend |
|---|---|
| Phase 1: Entities + migration | Phase 9: Zustand store |
| Phase 2: Repositories | Phase 8: MSAL + authService |
| Phase 3: Token service | Phase 10: Login page |
| Phase 4: Auth service | Phase 11: AuthGuard + routing |
| Phase 5: Controller | Phase 12: Top bar |
| Phase 6: CORS + Swagger | — |
| Phase 7: Tests | — |

---

## Key Security Rules — Must Not Be Violated

- Access token → **in-memory only** — never localStorage, never sessionStorage
- Refresh token → **HttpOnly cookie only** — never in response body, never readable by JS
- Refresh tokens → **hashed before storage** — `TokenHash` stores SHA-256 hash, never raw value
- Refresh token → **rotation on every use** — old token revoked immediately on use
- JWT signing key → **Azure Key Vault in prod** — `Jwt:SigningKey` in dev config only
- **No ASP.NET Core Identity** — no `UserManager`, no `SignInManager`, no identity tables
- Audit log → written on **every** login, logout, and token refresh

---

## Notes

- `[P]` = safe to parallelize (different files, no shared dependencies)
- JWT signing key must be 32+ characters — shorter keys fail `HmacSha256`
- `ep_refresh` cookie `Path="/api/v1/auth"` — browser only sends it on auth endpoints
- `AllowCredentials()` on CORS is required for HttpOnly cookies to work cross-origin
- Sprint 2 (Eventbrite + Registration Dashboard) depends on this sprint being fully validated
