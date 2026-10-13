# ASP.NET Core 10 Web API

A production-ready ASP.NET Core 10 Web API backend. Depending on what was selected when this project was generated, it may include JWT authentication, RBAC (Role-Based Access Control), WeChat Mini Program login, OpenTelemetry observability, Redis caching, and a companion unit test project.

## Features

| Feature                                                     | Present when |
| ------------------------------------------------------------ | ------------- |
| JWT authentication                                          | always (unless "No Authentication" was chosen) |
| Redis-backed JWT revocation (blacklist + permission cache)  | JWT option was "Redis" |
| Role-Based Access Control (RBAC)                            | RBAC was enabled |
| WeChat Mini Program login                                   | RBAC + WeChat were enabled |
| OpenTelemetry (traces, metrics, logs)                        | Telemetry was enabled |
| Unit test project (`Jyz.UnitTests`)        | Unit tests were enabled |

### Always included

- API versioning (`Asp.Versioning.Mvc`)
- FluentValidation with auto-discovery
- Mapster object mapping
- NLog logging
- Scalar + ASP.NET Core OpenAPI (`/scalar/v1` in development)
- Global exception handler → structured `BaseResponse<T>` envelope
- BCrypt password hashing

---

## Getting Started

### Step 1 — Restore dependencies

```bash
dotnet restore
```

### Step 2 — Configure secrets

Never commit secrets. Use user-secrets in development:

```bash
# JWT secret key (if JWT is present)
dotnet user-secrets set "JwtSettings:SecretKey" "your-secret-key-at-least-32-characters" --project Jyz

# Database connection (if RBAC is present) — DatabaseType must match the configured provider
dotnet user-secrets set "ConnectionStrings:DatabaseType" "MySql" --project Jyz
dotnet user-secrets set "ConnectionStrings:ConnectionString" "server=localhost;port=3306;user=root;password=secret;database=MyDb;" --project Jyz

# Redis (if JWT option is "Redis")
dotnet user-secrets set "Redis:Password" "your-redis-password" --project Jyz

# WeChat Mini Program (if WeChat is present)
dotnet user-secrets set "MiniProgram:AppId" "wx..." --project Jyz
dotnet user-secrets set "MiniProgram:Secret" "..." --project Jyz
```

### Step 3 — Generate and apply the first database migration (RBAC only)

No migrations ship with this project — they're database-provider-specific. Generate one for whichever provider is configured, then apply it:

```bash
pwsh reset-migrations.ps1
```

This is a thin wrapper around `dotnet ef migrations add Init` + `dotnet ef database update`. It's a squash (deletes existing migrations and regenerates one clean one) — fine to re-run while iterating on entities with no real data yet; once you have a production database, switch to incremental `dotnet ef migrations add <Name>`.

### Step 4 — Run

```bash
dotnet run
```

Open `https://localhost:{port}/scalar/v1` in your browser to explore the API.

---

## Feature Reference

### JWT Authentication

| Mode               | Description                                                   |
| ------------------- | --------------------------------------------------------------- |
| No Authentication  | No JWT authentication                                          |
| JWT                | JWT with in-memory token validation                            |
| JWT + Redis        | JWT (still stateless, not stored in Redis) + Redis user blacklist/permission cache for revocation |

**Note:** JWTs are always stateless and signed — a new token is issued on every login regardless of mode.
What the "Redis" mode adds is a side channel: a Redis-backed user blacklist (`BlacklistMiddleware` checks it
on every authenticated request, so a disabled/removed user is rejected even with a still-valid, unexpired
token) and a permission cache. This is what lets you forcibly revoke access before a token's natural expiry,
and it works across multiple app instances since the state lives in Redis rather than each instance's memory.

### RBAC (Role-Based Access Control)

When present:

- `User`, `Role`, `Permission` entities via EF Core (`AppDbContext`) — no migrations shipped, see Getting Started above
- `UserService`, `RoleService`, `PermissionService`
- User CRUD, Role CRUD, Permission management endpoints
- Auto-sync of `[Permission]` attribute metadata to the database on startup
- `AppDbContext` via `JfYu.Data` — actual EF Core provider depends on what was selected at generation time

### WeChat Mini Program Login

When present, enables WeChat Mini Program login alongside standard username/password login:

- `JfYu.WeChat` package
- `MiniProgram` config section in `appsettings.json`
- WeChat OAuth flow in `UserService`

### OpenTelemetry

When present, adds OpenTelemetry instrumentation:

- ASP.NET Core, HTTP client, EF Core, Redis, RabbitMQ traces (OTLP exporter)
- Prometheus metrics endpoint on `:9464/metrics`
- Log export via OTLP

### Unit Test Project

When present, `Jyz.UnitTests` includes:

- xUnit + Moq + FluentAssertions
- `Microsoft.AspNetCore.Mvc.Testing` for integration tests

---

## Versioning

The running app exposes `GET /api/version` to report the current version (read from assembly metadata, no external dependency).

---

## Project Structure

```
Jyz/
  Program.cs                     # App entry point
  Jyz.csproj    # Package references
  appsettings.json               # Configuration sections
  nlog.config                    # NLog logging configuration

  Attributes/
    PermissionAttribute.cs       # [Permission] attribute for RBAC endpoint metadata

  Constants/
    ResponseCode.cs              # Success / Failed / Error enum
    ErrorCode.cs                 # Business error codes with [Description]
    Gender.cs                    # Gender enum
    PlatformEnum.cs              # Web / Wechat login platform
    PermissionType.cs            # Menu / Button permission types  (RBAC)
    PermissionCodes.cs           # Constant permission code strings  (RBAC)

  Controllers/
    CustomController.cs          # Base controller — Ok<T>() / BadRequest(ErrorCode)
    AuthController.cs            # POST /auth/login, GET /auth/codes  (JWT + RBAC)
    UserController.cs            # GET/PUT /user  (JWT + RBAC)
    RoleController.cs            # Role CRUD  (JWT + RBAC)
    PermissionController.cs      # Permission CRUD + sync  (JWT + RBAC)

  Entity/
    AppDbContext.cs              # EF Core DbContext  (RBAC)
    User.cs / Role.cs / Permission.cs  (RBAC)

  Extensions/
    ServicesExtension.cs         # All AddCustomXxx() and UseXxx() extensions
    InjectionExtension.cs        # Service DI registrations
    OptionsExtension.cs          # IOptions<T> bindings
    MapsterExtension.cs          # Mapster mapping configuration
    EnumExtensions.cs            # GetDescription() for enums

  Migrations/                    # Not committed — run reset-migrations.ps1 to generate (RBAC)

  Model/
    BaseResponse.cs              # Generic API response envelope
    PagedResult.cs               # Pagination wrapper
    QueryRequest.cs              # Common paged query params
    User/                        # User-related DTOs
    Role/                        # Role-related DTOs  (RBAC)
    Permission/                  # Permission-related DTOs  (RBAC)

  Options/
    JwtSettings.cs               # Strongly-typed JWT config  (JWT)

  Services/
    CurrentUser.cs               # ICurrentUser — reads claims from HttpContext
    JwtService.cs                # JWT generation/validation  (JWT)
    UserService.cs               # User management + login  (RBAC)
    RoleService.cs               # Role management  (RBAC)
    PermissionService.cs         # Permission management + sync  (RBAC)
    Interfaces/                  # Service interfaces

  Validations/
    UpdateUserRequestValidation.cs     (RBAC)
    CreateRoleRequestValidation.cs     (RBAC)
    UpdateRoleRequestValidation.cs     (RBAC)
    CreatePermissionRequestValidation.cs  (RBAC)
    UpdatePermissionRequestValidation.cs  (RBAC)

Jyz.UnitTests/   (if unit tests were enabled)
  Jyz.UnitTests.csproj

reset-migrations.ps1             # Generates the first migration + applies it (RBAC)
```

---

## Development Notes

- Secrets must never be committed — use `dotnet user-secrets` in development.
- No migrations are committed or shipped — they're database-provider-specific. Run `reset-migrations.ps1`. Never edit generated migration files manually.
- Enum display text lives in `Resources/EnumMessages.resx` (English) and `EnumMessages.<culture>.resx` variants; `.GetDescription()` resolves the language per request via `Accept-Language`. Add a language by adding a new `.resx` and registering the culture.
- Throw `BusinessException(ErrorCode.XxxError)` from service layer; the global exception handler converts them to structured `BadRequest` responses.
- Adding a new feature module (entity, DTOs, service, controller, etc.)? See [`docs/fullstack-feature-guide.md`](../docs/fullstack-feature-guide.md) at the repository root for a complete step-by-step workflow.
