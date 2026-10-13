# AGENTS.md

This file provides guidance to AI coding assistants when working with this project.

## Tech Stack

- **Runtime**: .NET 10 / ASP.NET Core 10
- **ORM**: Entity Framework Core via `JfYu.Data` (wraps EF Core with read/write context splitting); see `JfYu.Data.Mysql` / `JfYu.Data.SqlServer` / `JfYu.Data.PostgreSQL` for the configured provider
- **Object mapping**: Mapster (`Adapt<T>()`)
- **Validation**: FluentValidation (`AbstractValidator<T>`)
- **Authentication**: JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- **Password hashing**: BCrypt.Net-Next (`BCrypt.Net.BCrypt.HashPassword / Verify`)
- **Logging**: NLog + `NLog.Web.AspNetCore` (config: `nlog.config`)
- **API docs**: Scalar + ASP.NET Core OpenAPI (`/scalar/v1` in development)
- **API versioning**: `Asp.Versioning.Mvc`
- **HTTP client**: `JfYu.Request`
- **Cache**: `JfYu.Redis` (optional)
- **WeChat Mini Program**: `JfYu.WeChat` (optional)
- **Observability**: OpenTelemetry (optional)

## Optional Capabilities

This project may or may not include the following, depending on what was selected when it was generated:

| Capability    | Enables                                                           |
| -------------- | ------------------------------------------------------------------ |
| JWT           | JWT authentication, `IJwtService`, `JwtSettings` config           |
| JWT + Redis   | Redis-backed revocation (user blacklist) + permission cache alongside JWT — JWTs stay stateless, not stored in Redis (`JfYu.Redis`) |
| RBAC          | Role-based access control, EF Core, User/Role/Permission entities |
| Telemetry     | OpenTelemetry traces, metrics, logs (OTLP exporter)               |
| WeChat        | WeChat Mini Program login (`JfYu.WeChat`)                         |

If a capability wasn't selected, its corresponding files simply aren't present in this project — there's nothing to toggle at runtime.

## Project Structure

```text
Jyz/
  Program.cs                     # App entry point; registers all extensions
  Jyz.csproj    # Package references
  appsettings.json               # Base config
  nlog.config                    # NLog logging configuration

  Attributes/
    PermissionAttribute.cs       # [Permission(code)] — IAsyncAuthorizationFilter, checks Redis/JWT claims (RBAC)

  Constants/
    ResponseCode.cs              # Enum: Success, Failed, ...
    ErrorCode.cs                 # Enum for business error codes (has [Description] attributes)
    Gender.cs                    # Gender enum
    PlatformEnum.cs              # Login platform enum (Normal, Wechat)
    PermissionType.cs            # Menu / Button permission types (RBAC)
    PermissionCodes.cs           # Permission code string constants (RBAC)
    RedisKey.cs                  # Redis key constants (JWT+Redis)

  Controllers/
    CustomController.cs          # Base controller — wraps Ok()/BadRequest() in BaseResponse<T>
    AuthController.cs            # Login endpoint
    UserController.cs            # User CRUD (RBAC)
    RoleController.cs            # Role CRUD + permission assignment (RBAC)
    PermissionController.cs      # Permission CRUD + menu sync (RBAC)
    DictTypeController.cs        # System dictionary type CRUD (RBAC)
    DictItemController.cs        # System dictionary item CRUD (RBAC)
    AuditLogController.cs        # Operation audit log query (RBAC)
    LoginLogController.cs        # Login audit log query (RBAC)
    RedisCacheController.cs      # Redis key inspection/removal (JWT+Redis)
    VersionController.cs         # GET /api/version — reads assembly metadata
    V2/                          # Example of a versioned controller (Asp.Versioning)

  Entity/
    AppDbContext.cs              # EF Core DbContext (RBAC)
    User.cs / Role.cs / Permission.cs / DictType.cs / DictItem.cs / AuditLog.cs / LoginLog.cs

  Exceptions/
    BusinessException.cs         # Thrown by services for domain errors

  Extensions/
    ServicesExtension.cs         # All AddCustomXxx() extension methods
    InjectionExtension.cs        # AddCustomInjection() — service DI registrations
    OptionsExtension.cs          # AddCustomOptions() — binds IOptions<T>
    MapsterExtension.cs          # AddMapster() — Mapster configuration
    EnumExtensions.cs            # GetDescription() for enums

  Infrastructure/
    AuditInterceptor.cs          # EF SaveChanges interceptor — writes AuditLog rows (RBAC)
    DbHealthCheck.cs             # ASP.NET Core health check for the DB connection
    BlacklistMiddleware.cs       # Rejects requests using a blacklisted/removed JWT (JWT+Redis)

  Migrations/                    # EF Core migrations — do NOT edit manually. Not committed —
                                  # run `reset-migrations.ps1` to generate the first one.

  Model/
    BaseResponse.cs              # Generic API response envelope
    PagedResult.cs / QueryRequest.cs   # Pagination wrapper + common paged query params
    <Feature>/                   # One subdirectory PER FEATURE, e.g. Model/Role/, Model/DictType/
      Create<Name>Request.cs / Update<Name>Request.cs / Query<Name>Request.cs / <Name>Response.cs

  Options/
    JwtSettings.cs               # Strongly-typed JWT config

  Resources/
    EnumMessages.resx / EnumMessages.zh-CN.resx  # Localized enum descriptions

  Services/
    Interfaces/                  # Service interfaces (IUserService, IRoleService, etc.)
    UserService.cs               # Implements Service<User, AppDbContext> from JfYu.Data
    RoleService.cs
    PermissionService.cs
    DictTypeService.cs / DictItemService.cs
    AuditLogService.cs / LoginLogService.cs
    RedisCacheService.cs         # (JWT+Redis)
    JwtService.cs                # JWT token generation/validation
    CurrentUser.cs                # ICurrentUser — resolves user from HttpContext claims

  Validations/
    CreateUserRequestValidation.cs / UpdateUserRequestValidation.cs / ...one per feature
```

**Important — DTO placement:** DTOs live in a per-feature subdirectory under `Model/<FeatureName>/` (e.g. `Model/Role/CreateRoleRequest.cs`), **not** in generic `Model/Request/` / `Model/Response/` folders.

## Architecture Patterns

### Response Envelope

All API responses are wrapped in `BaseResponse<T>`:

```csharp
public class BaseResponse<T>
{
    public ResponseCode Code { get; set; }   // Success | Failed
    public string Message { get; set; }
    public ErrorCode? ErrorCode { get; set; }
    public T? Data { get; set; }
}
```

Controllers inherit from `CustomController` (not `ControllerBase` directly) which provides `Ok<T>(data)` and `BadRequest(ErrorCode)` helpers that automatically wrap results.

### Service Layer

Services extend `Service<TEntity, TDbContext>` from `JfYu.Data` which provides:

- `AddAsync`, `UpdateAsync`, `RemoveAsync(predicate)` (soft delete — sets `Status = Disable`), `HardRemoveAsync(predicate)` (hard delete)
- `GetOneAsync(expression)`, `GetListAsync(expression)`, `GetSelectListAsync(selector)`
- Automatic read/write DB context splitting when `ReadonlyDBContext<T>` is registered

**Note:** `GetPagedAsync(QueryRequest)` is **not** inherited — each service defines its own, building the query and calling the `IQueryable.ToPagedAsync()` extension from `JfYu.Data.Extension` (see `RoleService.GetPagedAsync`).

### Adding a New Feature

1. **Entity** — add to `Entity/` and register `DbSet<T>` in `AppDbContext`.
2. **Migration** — run `dotnet ef migrations add <Name>` then `dotnet ef database update`.
3. **DTOs** — create `Model/<Name>/` and add `Create<Name>Request.cs`, `Update<Name>Request.cs`, `<Name>Response.cs` there.
4. **Validation** — add `Validations/CreateXxxRequestValidation.cs` implementing `AbstractValidator<CreateXxxRequest>`.
5. **Service** — add interface in `Services/Interfaces/`, implement in `Services/`.
6. **DI registration** — register the service in `InjectionExtension.AddCustomInjection()`.
7. **Controller** — inherit `CustomController`, inject service, use `Ok<T>()` / `BadRequest(ErrorCode)`.

For the complete, tool-agnostic workflow (DB schema review checkpoints, error codes, frontend wiring, tests), see [`docs/fullstack-feature-guide.md`](../docs/fullstack-feature-guide.md) at the repository root.

### Object Mapping

Use Mapster's `Adapt<T>()` for DTO ↔ entity conversion. Custom mapping configurations go in `MapsterExtension.cs`.

### Validation

FluentValidation validators are auto-discovered and registered via `AddCustomFluentValidation()`. Model validation errors are automatically returned as `BaseResponse<Dictionary<string, ModelErrorCollection>>` with `ErrorCode.ValidationError`.

### Authentication & Authorization

- JWT tokens are issued by `JwtService` and validated by `AddCustomAuthentication()`.
- Use `[Permission(PermissionCodes.Xxx)]` on controllers/actions — do **not** use `[Authorize]` directly; `PermissionAttribute` checks Redis (if JWT+Redis) or JWT claims (RBAC).
- `ICurrentUser` is injectable to get the currently authenticated user's claims.

## Build & Run Commands

```bash
# Restore dependencies
dotnet restore Jyz.csproj

# Build
dotnet build Jyz.csproj

# Run (development)
dotnet run --project Jyz

# Run unit tests
dotnet test Jyz.UnitTests/Jyz.UnitTests.csproj

# EF Core migrations (regenerate — no migrations are committed, see Migrations/ note above)
pwsh reset-migrations.ps1              # deletes existing migrations, adds "Init", applies it
pwsh reset-migrations.ps1 -SkipDatabaseUpdate   # generate only, don't touch the database
```

## Configuration

`appsettings.json` (and `appsettings.Development.json` for overrides):

| Section          | Purpose                                                                   |
| ---------------- | -------------------------------------------------------------------------- |
| `JwtSettings`     | SecretKey, Issuer, Audience, Expires (seconds)                            |
| `ConnectionStrings` | `DatabaseType` + `ConnectionString`, bound directly onto `JfYu.Data`'s options — **not** `ConnectionStrings.DefaultConnection`. MySql/MariaDB only: optional `Version` (skips a live-connection server-version auto-detect at `dotnet ef` design-time) |
| `Redis`           | Host, Port, Password, DbIndex, Timeout, Ssl                               |
| `MiniProgram`     | WeChat AppId + Secret                                                     |

Sensitive values should use **user-secrets** in development:

```bash
dotnet user-secrets set "JwtSettings:SecretKey" "<value>" --project Jyz
```

## Key Conventions

- Enum display text is resolved from `Resources/EnumMessages.resx` (English) and culture variants such as `EnumMessages.zh-CN.resx`; `.GetDescription()` picks the language from the request's `Accept-Language` header. To add a language (e.g., French, German), add `EnumMessages.<culture>.resx` and register the culture in `AddCustomLocalization()`.
- Throw `BusinessException(ErrorCode.XxxError)` from service layer for domain errors; the global exception handler converts these to structured `BadRequest` responses.
- All controller actions return `IActionResult`; never return raw objects or use `ActionResult<T>`.
- Async/await throughout — no `.Result` or `.Wait()`.
- Nullable reference types are enabled (`<Nullable>enable</Nullable>`).
