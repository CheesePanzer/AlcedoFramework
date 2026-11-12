# Alcedo.Framework.Identity

This module provides **authentication, authorization, and session management** for the Alcedo platform. It is built on top of `Alcedo.Core` and `Alcedo.Framework.Shared`, using Redis for session state and JWT Bearer tokens for API authentication.

The module follows a layered architecture:

| Layer | Responsibility |
|-------|----------------|
| **Presentation** | `AuthSessionMiddleware`, `AlcedoRbacHandler`, endpoint-permission filters |
| **Controller** | `AuthController`, `UserController`, cookie/response wrappers |
| **Application** | `LoginServiceComponent`, `AuthApplicationService`, DTOs, commands |
| **Domain** | `UserAgg`, `RoleAgg`, `PermEntity`, `HistoryUserEntity`, domain events |
| **Infrastructure** | Redis stores, EF repositories, seeders, `PasswordHasher`, `JwtProvider` |

---

## Table of Contents

- [Authentication Flow](#authentication-flow)
  - [Password Login](#password-login)
  - [TOTP Verification](#totp-verification)
  - [Token Refresh](#token-refresh)
  - [Logout](#logout)
- [Session Validation Pipeline](#session-validation-pipeline)
- [RBAC Authorization](#rbac-authorization)
  - [AuthSessionMiddleware](#authsessionmiddleware)
  - [AlcedoRbacHandler](#alcedorbachandler)
- [RBAC Endpoint Scanning](#rbac-endpoint-scanning)
  - [Resource & Operation Attributes](#resource--operation-attributes)
  - [EndpointPermissionCenter](#endpointpermissioncenter)
  - [PermissionSeeder](#permissionseeder)
- [Key Data Structures](#key-data-structures)

---

## Authentication Flow

All authentication endpoints are exposed through `AuthController` (area: `identity`). The controller itself is thin; orchestration is delegated to `AuthApplicationService` and `LoginServiceComponent`.

### Password Login

**Endpoint**: `POST /api/v1/identity/login`

**Flow**:

1. **Controller** receives `AuthCommand` (loginName + password).
2. `AuthApplicationService.LoginAsync` delegates to `LoginServiceComponent.LoginAsync`.
3. `InternalVerifyCredentialsAsync` fetches the user by login name:
   - If user not found or disabled → returns failure (goes through throttle logic to prevent username enumeration).
   - If password mismatch → returns failure (throttle counter incremented).
   - If password correct → proceeds.
4. `ProcessLoginFailureAsync` evaluates the throttle counter. If it exceeds the permanent-lock threshold, the account is locked.
5. On success, `PrepareUserSessionAsync` generates a new `CurrentSession` GUID under a DB transaction.
6. `FinalizeLoginOnSuccess` creates:
   - A **JWT access token** (expires in `AccessTokenValidMinutes`).
   - A **refresh token** stored in Redis (expires in `RefreshTokenValidMinutes`).
   - A `UserCacheDto` written to Redis.
7. The refresh token is returned to the client via `IRefreshTokenCookieProvider.Set` (HttpOnly, Secure, SameSite=Strict).

**TOTP branch**: If `user.UseTwoFactorAuthenticator == true`, the password phase returns an empty `AccessToken` and a `PendingToken`. The actual JWT is deferred until TOTP verification.

### TOTP Verification

**Endpoint**: `POST /api/v1/identity/twofactor`

**Flow**:

1. **Controller** receives `TotpCommand` (pendingToken + totpCode).
2. `VerifyTotpAsync` retrieves the pending `LoginContextData` from Redis by `pendingToken`.
3. Validates the user still exists and `CanAccessContext`.
4. Verifies the TOTP code against `user.TwoFactorSecret`.
5. On success, completes the login (same `FinalizeLoginOnSuccess` path), deletes the pending context, and returns the JWT + refresh token.

### Token Refresh

**Endpoint**: `POST /api/v1/identity/jwt:refresh`

**Flow**:

1. **Controller** reads the refresh token from the cookie via `IRefreshTokenCookieProvider.Get`.
2. `RefreshJwtAsync` validates the refresh token in Redis (`RefreshTokenStore.ValidateTokenAsync`).
3. Validates the user's cached session is still active (`CurrentSession` not null).
4. **Rotates** the refresh token atomically (old token deleted, new token written with remaining TTL).
5. Generates a new JWT signed with the current session.
6. Returns the new access token; the new refresh token is set in the cookie.

> **Note**: Token rotation is protected by a distributed lock (`ApplicationLockProvider`) scoped to the user, preventing race conditions during concurrent refresh attempts.

### Logout

**Endpoint**: `PUT /api/v1/identity/logout`

**Flow**:

1. `LogoutAsync` executes a DB transaction to call `user.LogOut()`, which invalidates `CurrentSession`.
2. Clears Redis state: deletes the refresh token mapping and user cache.
3. Deletes the refresh token cookie.

---

## Session Validation Pipeline

Every authenticated request passes through two gates:

### 1. AuthSessionMiddleware

Runs **before** MVC authorization. Validates:

- Request is authenticated (`context.User.Identity.IsAuthenticated`).
- JWT carries `NameIdentifier` (userId) and `currentSession` claims.
- `userId` is a valid GUID.
- User cache exists in Redis.
- `cachedUser.CanAccessContext == true`.
- `cachedUser.CurrentSession` matches the JWT's `currentSession` claim.

If any check fails, the middleware either challenges (401) or throws `IdentityErrSessionInvalid`.

On success, the `UserCacheDto` is attached to `HttpContext.Items[HttpContextUserStoreKey.Key]` so downstream handlers do not re-fetch it from Redis.

### 2. AlcedoRbacHandler

Runs **after** `AuthSessionMiddleware`, as part of ASP.NET Core's authorization pipeline. It enforces **endpoint-level permissions**.

---

## RBAC Authorization

### AuthSessionMiddleware

```
Request → AuthSessionMiddleware
    ├── AllowAnonymous endpoint? → skip
    ├── Not authenticated? → 401 Challenge
    ├── Missing/invalid claims? → 401 Challenge
    ├── User locked? → 401 IdentityErrInvalidUser
    ├── Session mismatch? → 401 IdentityErrSessionInvalid
    └── OK → attach UserCacheDto to HttpContext.Items
```

### AlcedoRbacHandler

```
Request → AuthSessionMiddleware (passed)
    → AlcedoRbacHandler
        ├── AllowAnonymous or SkipPermissionCheck? → Succeed
        ├── Read UserCacheDto from HttpContext.Items (or fallback to Redis)
        ├── User locked? → 401
        ├── Root user? → Succeed
        ├── Resolve endpoint key from Controller+Action
        ├── Check role cache: any role allows this endpoint?
        │   └── No? → 403 IdentityErrNotPermitted
        └── Yes? → Succeed
```

**Endpoint key resolution**:

```
{area}.{resource}.{operation}
```

- `area`: from `[Area("...")]` on controller or action.
- `resource`: from `[Resource("Code", "Description")]` on controller.
- `operation`: from `[Operation("Code", "Description")]` on action.

If no `Area` is defined, the key format collapses to `{resource}.{operation}`.

**Role cache check**: `IRoleCacheProvider.CheckPermissionByRoleIdsAsync` iterates the user's `RoleIds`, fetches each `RoleCacheDto` from Redis, and checks if `AllowedEndpoints` contains the resolved key (ordinal-ignore-case).

---

## RBAC Endpoint Scanning

Permissions are not hard-coded. They are **discovered automatically** from controller/action metadata at startup and synchronized to the database.

### Resource & Operation Attributes

Every controller and action must declare these attributes:

```csharp
[Area("identity")]
[Resource("Auth", "Authorization")]
public sealed class AuthController : AlcedoApiAreaControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [Operation("Login")]
    public async Task<ActionResult<AuthTokenPasswordPhaseResult>> Login(...) { }

    [HttpPost("twofactor")]
    [AllowAnonymous]
    [Operation("TotpLogin", "Enter Totp Code")]
    public async Task<ActionResult<AuthTokenTotpPhaseResult>> TotpLogin(...) { }
}
```

- **`[Resource]`** (class-level): Groups endpoints under a business resource. Code is used in the permission key; Description is used in UI/admin panels.
- **`[Operation]`** (method-level): Identifies a specific action on the resource. Code is used in the permission key; Description provides human-readable context.
- **`[SkipPermissionCheck]`**: Marks an endpoint as exempt from RBAC (still requires authentication via `AuthSessionMiddleware`).
- **`[AllowAnonymous]`**: Marks an endpoint as fully public (bypasses both session and RBAC checks).

### EndpointPermissionCenter

A singleton service that scans all loaded assemblies at startup and builds an in-memory cache of endpoint keys.

**Key construction**:

```csharp
var key = string.IsNullOrWhiteSpace(area)
    ? $"{resource}.{operation}"
    : $"{area}.{resource}.{operation}";
```

Stored as lowercase in a `ConcurrentDictionary<(Type, MethodInfo), string>`.

At runtime, `AlcedoRbacHandler` resolves the current request's `ControllerActionDescriptor` and looks up the key via `_center.GetKey(controllerType, methodInfo)`.

### PermissionSeeder

Runs at application startup (typically in the Host layer). It:

1. Scans all controller types using the same reflection logic as `EndpointPermissionCenter`.
2. Extracts `PermEntity` records from `[Resource]` + `[Operation]` metadata.
3. **Syncs to database**:
   - **New** permissions → `INSERT`
   - **Existing** permissions → `UPDATE` description
   - **Removed** permissions (no longer present in code) → `DELETE` cascaded to `RolePermissionsRelation`

This ensures the permission table always matches the actual API surface. There is no manual permission registration.

---

## Key Data Structures

### Redis Key Space

| Store | Key Pattern | Value |
|-------|-------------|-------|
| `RefreshTokenStore` | `User:{userId}` | SHA256 hash of current refresh token |
| `RefreshTokenStore` | `Token:{hash}` | `userId` |
| `UserCacheStore` | `Cache:{userId}` | Serialized `UserCacheDto` |
| `RoleCacheStore` | `Cache:{roleId}` | Serialized `RoleCacheDto` |
| `LoginThrottleStore` | `Throttle:{loginName}` | Failure count |
| `LoginContextStore` | `LoginContext:{pendingToken}` | Serialized `LoginContextData` |

### UserCacheDto (Redis)

```csharp
public class UserCacheDto
{
    public Guid Id { get; set; }
    public string LoginName { get; set; }
    public string Name { get; set; }
    public Guid? CurrentSession { get; set; }
    public List<Guid> RoleIds { get; set; }
    public bool IsRootUser { get; set; }
    public bool CanAccessContext { get; set; }
}
```

### RoleCacheDto (Redis)

```csharp
public class RoleCacheDto
{
    public Guid RoleId { get; set; }
    public bool IsEnabled { get; set; }
    public HashSet<string> AllowedEndpoints { get; set; }
}
```

### JWT Claims

| Claim | Source | Purpose |
|-------|--------|---------|
| `currentSession` | `UserAgg.CurrentSession` | Tied to a single login session; invalidated on logout or concurrent login |
| `NameIdentifier` | `UserAgg.Id` | User identity |
| `Name` | `UserAgg.LoginName` | Display name |
