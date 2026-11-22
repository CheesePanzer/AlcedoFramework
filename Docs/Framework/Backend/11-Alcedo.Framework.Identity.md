# Alcedo.Framework.Identity

Identity provides **JWT authentication, server-side session validation, and endpoint-level RBAC**. JWTs carry user/session identifiers; PostgreSQL stores users and roles, while Redis supports session/cache lookup and refresh-token mappings. Use the Contracts project for cross-module identity queries rather than coupling a business module to Identity repositories.

---

## Table of Contents

- [Registration and Configuration](#registration-and-configuration)
- [Bootstrap and First Login](#bootstrap-and-first-login)
- [Password and TOTP Login](#password-and-totp-login)
- [Authenticated Requests and Refresh](#authenticated-requests-and-refresh)
- [Users Roles and Permissions](#users-roles-and-permissions)
- [Protect a Business API](#protect-a-business-api)
- [Profile and Account Operations](#profile-and-account-operations)
- [Troubleshooting and Further Reading](#troubleshooting-and-further-reading)

---

## Registration and Configuration

Register `.AddAlcedoFrameworkBase().AddAlcedoIdentity()` in the module registry and call `UseAlcedoIdentityMiddlewares()` after the Core middleware setup. This adds authentication, session validation, and authorization in that order. See the [complete host skeleton](00-Alcedo.Framework-Overview.md#register-the-host).

The Identity-specific configuration sections are:

```json
{
  "JwtOptions": {
    "Issuer": "Alcedo",
    "Audience": "AlcedoUser",
    "Algorithm": "HS256",
    "Key": "REPLACE_WITH_A_PRIVATE_SIGNING_KEY"
  },
  "SecurityOptions": {
    "LoginFailedTTLSeconds": 60,
    "AccountLockThresholdCount": 5,
    "LoginContextValiditySeconds": 180,
    "AccessTokenValidMinutes": 15,
    "RefreshTokenValidMinutes": 480,
    "CookieSecureOnly": true
  },
  "TotpOptions": { "Issuer": "Alcedo" },
  "RbacSchemeOptions": { "RoleCacheExpiryMinutes": 120 },
  "AlcedoColumnEncryptorOptions": {
    "Key": "REPLACE_WITH_A_BASE64_ENCODED_32_BYTE_AES_KEY"
  }
}
```

These are configuration shapes, not usable secrets. Store real values outside committed configuration. The JWT implementation uses `Encoding.UTF8.GetBytes(JwtOptions.Key)`; it does not Base64-decode that string. Column encryption does Base64-decode its key. Treat the two keys as distinct configuration values.

PostgreSQL/Redis, message transport, migrations, and Core settings are also required. For the browser client, configure the correct allowed CORS origin and credential handling. The refresh cookie is HttpOnly, uses **SameSite=Lax**, and uses `Secure=CookieSecureOnly`; it is not a token exposed through an `X-Refresh-Token` request header.

---

## Bootstrap and First Login

After applying the schema, `app.SeedAlcedoIdentity()` creates a root user if none exists. The supplied development seeder uses `admin` / `admin`, enables the account, and sets `RequiresPasswordChange`.

Change the seeded password before exposing the system. The login response's `RequirePasswordReset` flag tells clients to show the change-password workflow. The current implementation does not issue a restricted first-login JWT, so that flag is not a server-enforced prohibition on all other business API calls.

The root user bypasses role endpoint checks. Use it to set up roles and ordinary users, then test permissions using an ordinary account rather than assuming a successful root request proves the role configuration works.

---

## Password and TOTP Login

All Auth routes include the controller segment **`/auth`**:

```http
POST /api/v1/identity/auth/login
Content-Type: application/json
X-Idempotency-Key: a-new-unique-request-key

{"loginName":"admin","password":"YOUR_CURRENT_PASSWORD"}
```

Use a new idempotency key for a new POST attempt. Reusing one can produce a duplicate-request error before the login action is evaluated.

For a password-only login, the response contains `accessToken`, `useTotp=false`, `pendingToken=null`, `requirePasswordReset`, and `validUntil`. The refresh token is written into the `X-Refresh-Token` cookie. **`ValidUntil` describes the refresh/session window**, not the access token's JWT `exp` time; access lifetime is controlled separately by `AccessTokenValidMinutes`.

If TOTP is enabled, the password phase returns `useTotp=true` and a `pendingToken`, without an access token. Complete the flow with:

```http
POST /api/v1/identity/auth/twofactor
Content-Type: application/json
X-Idempotency-Key: another-new-unique-request-key

{"pendingToken":"PENDING_TOKEN_FROM_LOGIN","totpCode":"123456"}
```

On success, store the returned access token and retain the response cookie. Pending contexts expire after `LoginContextValiditySeconds`; an invalid TOTP attempt deletes that context, so start again with password login when necessary.

Failed password/TOTP checks increment a Redis counter scoped to the login name. At the configured threshold, the enabled account is locked in the domain. The counter's TTL is not the same as automatically unlocking the persisted account; unlocking is an administrative action.

---

## Authenticated Requests and Refresh

Send the access token as:

```http
Authorization: Bearer ACCESS_TOKEN
```

`AuthSessionMiddleware` validates the user and checks that the JWT's `currentSession` matches `UserCacheDto.CurrentSession`. A cryptographically valid token can therefore still be rejected after logout, password reset, or another login replaces its session.

The RBAC handler then checks account eligibility, root status, and role permissions. `SkipPermissionCheck` bypasses the endpoint permission check, not JWT authentication or normal session validation.

Refresh uses the HttpOnly cookie:

```http
POST /api/v1/identity/auth/jwt:refresh
X-Idempotency-Key: a-new-refresh-request-key
Cookie: X-Refresh-Token=REFRESH_TOKEN
```

The operation returns a new access token and writes a replacement refresh cookie. Preserve cookies between requests; a browser client needs credentialed requests with a compatible origin/scheme setup. Serialize refresh attempts per session. The application uses a user-scoped exclusive lock around rotation, but the underlying Redis store performs multiple operations, not one atomic Lua transaction.

Logout is `PUT /api/v1/identity/auth/logout`. It reads/deletes the refresh cookie and invalidates the corresponding server session. The client should also clear its stored access token.

---

## Users Roles and Permissions

Users and roles reuse the [Shared CRUD/state API](10-Alcedo.Framework.Shared.md#expose-an-api). Their prefixes are `/api/v1/identity/users` and `/api/v1/identity/roles`; newly created records begin in Draft and need enabling for ordinary access.

A typical setup sequence is:

1. Query `GET /api/v1/identity/perms` to obtain the actual endpoint keys.
2. Create a role, assign the required keys, and enable the role.
3. Create a user, assign the enabled role, and enable the user.
4. Log in as that user and verify permitted and denied endpoints.

Additional assignment operations use JSON arrays:

```text
PATCH /api/v1/identity/roles/{id}/role-perms:assign   ["area.resource.operation"]
PATCH /api/v1/identity/roles/{id}/role-perms:delete   ["area.resource.operation"]
PATCH /api/v1/identity/roles/{id}/role-perms:clear
PATCH /api/v1/identity/users/{id}/user-roles:assign   ["ROLE_GUID"]
PATCH /api/v1/identity/users/{id}/user-roles:delete   ["ROLE_GUID"]
PATCH /api/v1/identity/users/{id}/user-roles:clear
```

Non-root permission assignment is constrained by the operator's own effective permissions; it is not a mechanism to grant arbitrary access the operator does not have. User domain methods also reject several self-targeted administrative changes.

Permission changes propagate through cache-refresh events. For an explicitly requested immediate role-cache refresh, use `PUT /api/v1/identity/roles/{id}/cache:refresh`; a user can request `POST /api/v1/identity/auth/cache:refresh` for their own user cache. Both still need their applicable authentication/authorization checks.

---

## Protect a Business API

Use a Framework API base and stable resource/operation names:

```csharp
using Alcedo.Core.Presentation;
using Alcedo.Framework.Shared.Controller;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

[Area("catalog")]
[ApiVersion("1.0")]
[Resource("Products", "Product catalog")]
public sealed class ProductsController : AlcedoFmApiAreaControllerBase
{
    [HttpGet]
    [Operation("List", "List products")]
    public IActionResult List() => Ok(new { message = "Authorized" });
}
```

Its route is `/api/v1/catalog/products` and its endpoint permission key is `catalog.products.list`. `EndpointResourceOperationCenter` builds lowercase `{area}.{resource}.{operation}` keys (or `{resource}.{operation}` without an area) from controller metadata. It scans code; there is no required database permission seeder or `PermEntity` inventory to create first.

Register the controller's owning module so endpoint scanning sees it. Keep permission names stable when evolving endpoints. Use `[AllowAnonymous]` only for an intentionally public action. Add `[SkipPermissionCheck]` for an authenticated self-service action that should not require a separate role permission.

---

## Profile and Account Operations

Under `/api/v1/identity/auth`:

- `GET /profile` returns the current user's profile.
- `POST /password:change` accepts `oldPassword` and `newPassword`; the domain clears the current session, so sign in again afterward.
- `PATCH /twofactor:enable` returns an authenticator setup URI.
- `PATCH /twofactor:disable` disables the current user's TOTP.

The current TOTP enable operation persists the secret immediately; it is not a pending setup/confirm-code API. The disable operation relies on the current session rather than requiring a separate password/TOTP body. Design the client workflow around those actual behaviors.

Administrative user operations include `POST /users/{id}/password:reset`, `PATCH /users/{id}/status:lock`, `PATCH /users/{id}/status:unlock`, `PATCH /users/{id}/status:archive`, and `PATCH /users/{id}/twofactor:disable`, all under `/api/v1/identity`.

---

## Troubleshooting and Further Reading

- **401 with an unexpired JWT**: inspect session replacement, account eligibility, and cookie/refresh state. `exp` alone does not prove the session is valid.
- **403 for an ordinary user**: check enabled roles, actual endpoint keys from `/perms`, and cache propagation. A root request bypasses this check.
- **Refresh cookie missing**: inspect HTTPS, `CookieSecureOnly`, SameSite behavior, CORS, and client credentials. Do not disable secure cookies for a deployed HTTPS environment merely to mask an origin mismatch.
- **Duplicate POST rejected**: create a fresh `X-Idempotency-Key` for a new operation.
- **TOTP secret cannot be read after configuration changes**: the encrypted column depends on `AlcedoColumnEncryptorOptions.Key`; replacing that key does not migrate existing ciphertext.

See [Identity Contracts](../../../Src/Framework/Backend/Alcedo.Framework.Identity.Contracts), [AuthController](../../../Src/Framework/Backend/Alcedo.Framework.Identity/Controller/AuthController.cs), [Shared Controller Bases](../../../Src/Framework/Backend/Alcedo.Framework.Shared/Controller/AlcedoFrameworkApiControllerBase.cs), and the [Testing Strategy](../../Testing/00-Testing-Strategy.md) for the existing API verification scope.
