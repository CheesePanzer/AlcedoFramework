# Testing Strategy

## Purpose

This document defines the testing layers for Alcedo Framework, records the
current coverage, and separates framework responsibilities from product-level
testing in Hercules and other consuming projects.

## Testing Layers

| Layer | Current status | Primary owner |
|---|---|---|
| Unit tests | Substantially covered | Alcedo Framework |
| Architecture tests | Partially covered | Alcedo Framework |
| Black-box API functional tests | In progress: Identity foundation implemented | Alcedo Framework Testkit |
| UI end-to-end tests | Missing | Framework sample and consuming projects |
| Business acceptance tests | Out of framework scope | Consuming projects |

## Current Coverage

The existing test suite covers the main isolated behaviors in:

- Core Domain, Application, Infrastructure, and Presentation
- State machines and aggregate metadata
- Search query and reference-integrity infrastructure
- Identity domain and application services
- RBAC handler and authentication session middleware
- Framework controller authorization conventions
- JuiceStore aggregate rules and MasterData state transitions

The following delivery paths have also been manually verified:

- Full solution build and test
- Backend and frontend Docker image builds
- Blazor static hosting through nginx
- Initial EF Core migration and idempotent SQL generation
- Sample Docker Compose configuration and image builds

Further class-level unit tests are not the current priority.

### Black-Box API Test Progress

The API test suite is maintained as an independent Python project:

```text
D:\python projects\AlcedoFrameworkTest
```

The project uses Python 3.12, `pytest`, and `httpx`. It contains a reusable
`alcedo_testkit` package organized by Framework module and executes requests
against a real running backend. Configuration is supplied through TOML and
environment variables; Framework endpoint paths remain part of the Testkit's
code-level contract.

The Identity suite currently contains 34 tests and the latest full run passes.
The Problem Details response uses the required `application/problem+json` media
type. Identity core-flow implementation is complete; new Identity tests are
deferred unless its public behavior changes.

Implemented coverage:

- Root login and authenticated profile access
- Rejection of invalid credentials
- Anonymous and authenticated endpoint behavior
- Root access to Roles, Users, and Permissions management endpoints
- Refresh-token rotation and rejection of the previous refresh token
- Logout invalidation of refresh and access tokens
- Invalidation of the previous session after a second login
- Refresh-cookie creation and deletion attributes
- Required and duplicate `X-Idempotency-Key` behavior
- `AllowAnonymous` and `SkipPermissionCheck` authentication semantics
- Problem Details status, fields, error code, and media-type contract
- Ordinary-user User -> Role -> Permission authorization propagation
- Immediate permission changes through explicit Role and User cache refresh
- Eventual permission changes after Role enable and disable events
- Permission revocation after removing a Role from a User
- Permission union across multiple Roles
- Rejection when a non-Root operator assigns a Permission it does not own
- Rejection of random, truncated, malformed, payload-modified, signature-modified,
  and `alg: none` JWTs
- Exactly-once Role creation for a sequentially reused successful idempotency key
- Lock and unlock behavior, including immediate access and Refresh Token revocation
- Disable and re-enable behavior, including immediate credential revocation
- First-login password-reset indicators, password change, and administrator reset
- Concurrent Refresh Token replay with exactly one successful rotation
- TOTP setup, password phase, invalid code rejection, pending-token replay
  rejection, valid second-factor completion, disablement, and direct login
- Wrong-password threshold, automatic account locking, active JWT rejection,
  administrator unlock, and restored login

The suite is now a functional security baseline rather than a formal security
certification. Authentication, account state, password, token tampering,
concurrent refresh, TOTP, brute-force locking, and core RBAC flows are covered.

## Functional Test Approach

Framework API tests should run against the published Sample backend as a real
service. They must not use `WebApplicationFactory`, `TestServer`, direct service
calls, repositories, or `DbContext` to perform business operations.

The functional test flow is:

```text
Docker Compose infrastructure
  -> database initialization
  -> published backend container
  -> black-box HTTP test scripts
```

Use Python with `pytest` and `httpx` for these tests. The scripts are independent
of Framework implementation types and accept the backend URL and credentials
through TOML configuration with environment-variable overrides.

Migration generation and execution remain part of normal development and
deployment verification. Dedicated migration tests are not currently planned.

## Framework Tests To Add

### 1. Identity And RBAC API Tests

Completed first priority. PostgreSQL, Redis, and the actual backend are exercised
only through HTTP.

Completed foundation:

- Login, refresh, and logout
- Unauthorized requests return 401
- Root authorization bypass behavior
- `AllowAnonymous` and `SkipPermissionCheck` semantics
- Session invalidation after logout and subsequent login
- Cookie, idempotency, and Problem Details HTTP contracts

Completed ordinary-user RBAC acceptance flow:

1. Root creates and enables a Role.
2. Root creates and enables a User.
3. Root assigns the Role to the User.
4. The ordinary user logs in.
5. Access to Roles without `identity.roles.pageview` returns 403.
6. Root assigns the endpoint permission to the Role.
7. Explicit Role and current-user cache refresh makes access succeed immediately.
8. Root removes the permission.
9. Explicit cache refresh makes access return 403 again.

This flow verifies the complete User -> Role -> Permission chain rather than
only Root bypass. Explicit refresh is an immediate-consistency contract. Role
status changes also verify that automatic cache events reach eventual
consistency without calling refresh endpoints.

Additional completed RBAC behavior:

- Removing a Role from a User revokes the Role's permissions.
- Disabling and re-enabling a Role eventually updates authorization.
- Permissions from multiple Roles are combined.
- A non-Root operator cannot assign a Permission it does not own.
- A rejected Permission assignment leaves the target Role unchanged.

### Identity Security Test Roadmap

The passing suite currently proves core functional behavior, but it must not be
treated as evidence that all Identity security boundaries are covered. Security
work is prioritized by the impact of a potential failure.

#### P0: Privilege Escalation And Credential Integrity

Implement these before describing the Identity module as security-complete:

1. Verify that a non-Root operator cannot delete a Permission it does not own
   from another Role.
2. Verify that a non-Root operator cannot clear a Role containing Permissions
   it does not own.
3. Verify that rejected delegated-administration operations leave the target
   User, Role, and Permission relations unchanged.
4. Reject modified JWT payloads, modified signatures, truncated tokens,
   malformed Base64, random bearer values, and `alg: none` tokens with 401 and
   without a 500 response.
5. Send concurrent refresh requests with the same Refresh Token and require
   exactly one success, one controlled rejection, and one usable rotated token.
6. Verify that lock, disable, archive, password change, and administrator
   password reset immediately invalidate existing access and Refresh Tokens.
7. Decide whether `RequiresPasswordChange` is informational or an enforced
   security boundary. If enforced, block ordinary API access until the password
   is changed and add a black-box acceptance test.

The Permission delegation tests are implemented and passing:

- A non-Root operator with `identity.roles.deletepermissions` can delete a
  Permission only when it is within the operator's effective Permission set.
- A non-Root operator with `identity.roles.clearpermissions` can clear a Role
  only when all affected Permissions are within the operator's effective set.

Rejected operations return 403 and leave the target Role unchanged.

Role ownership and management scope are intentionally not modeled at this time.
A non-Root operator with the relevant User-Role operation Permission may assign
or remove any enabled Role. This is accepted behavior, not a security failure.
Introducing explicit manageable Role IDs, hierarchy, or organization scope is
deferred until the Role governance model is defined. The current security
boundary is Permission-based: Role Permission mutations must not let an operator
grant, remove, or clear Permissions outside the operator's effective Permission
set.

#### P1: Account And Session Security

P1 user-state acceptance flows:

- A locked user cannot log in, and locking revokes an existing JWT session.
- Unlocking permits a new login.
- A disabled user cannot log in, and disabling revokes an existing JWT session.
- Re-enabling permits a new login.
- Administrator password reset revokes the existing session, rejects the old
  password, and accepts the generated password.
- First login returns `requirePasswordReset: true`.
- Profile returns the corresponding `requiresPasswordChange: true` value.
- Changing the password revokes the current session, rejects the old password,
  accepts the new password, and clears both password-reset indicators.

Additional P1 coverage:

- Compare an existing user's wrong-password response with an unknown-user
  response to prevent account enumeration through status or response content.
- Verify brute-force thresholds, automatic locking, active-session revocation,
  administrator unlock, and reset of the failure counter after successful login.
- Test malformed, random, replayed, cross-session, and expired Refresh Tokens.
- Verify that a newly rotated access token and Refresh Token are both usable,
  while all replaced credentials are rejected.
- Test successful write operations with duplicate and concurrent idempotency
  keys, ensuring that only one mutation and one audit action occur.
- Build an operation-level RBAC matrix for Role and User create, update, status,
  deletion, Role assignment, Permission management, password reset, cache
  refresh, and login-log access.
- Verify bounded event-driven cache propagation without explicit cache refresh
  for Role assignment, Role removal, Permission changes, and User state changes.
- Verify login and Identity audit logs, access control, eventual delivery, and
  absence of passwords, JWTs, Refresh Tokens, TOTP codes, and secrets.

The brute-force threshold test is implemented and passing. At the configured
threshold the account is locked, existing access and Refresh Tokens are revoked,
password login is rejected, and administrator unlock restores login.

#### P2: Defense In Depth

- TOTP secret rotation and administrative session-invalidation tests remain
  optional defense-in-depth coverage; the core TOTP login lifecycle is complete.
- Verify approved and unapproved CORS origins for credentialed refresh and
  logout requests, including preflight behavior and `Origin: null`.
- Verify production security-profile cookie requirements independently of local
  configuration: `Secure`, `HttpOnly`, SameSite, Path, and expiration.
- Test malformed JSON, duplicate properties, invalid content types, oversized
  inputs, unsupported methods, and long or Unicode credential values.
- Verify that login and profile responses never expose password hashes, TOTP
  secrets, current-session identifiers, cache data, or Refresh Tokens.
- Verify Root and self-protection rules for lock, disable, archive, Role
  modification, password reset, and TOTP administration.

Security-sensitive tests that use the single Root account must run serially.
The current session model invalidates an earlier Root session after a later Root
login, so parallel execution would create false failures and unreliable cleanup.
Cleanup must verify its responses to avoid leaving enabled users or privileged
roles in a shared test environment.

#### Implementation Complexity

Security priority and implementation complexity are independent. P0 identifies
the impact of a security failure; it does not imply that the test is difficult.
Implementation should take simple high-risk tests before lower-risk complex
tests.

Low complexity:

- P0 non-Root deletion or clearing of Permissions it does not own
- P1 lock, unlock, disable, enable, and credential revocation
- P1 first-login password-reset indicators
- P1 password change and administrator password reset
- P1 existing-user wrong-password versus unknown-user responses
- P1 malformed and random Refresh Token inputs
- P2 sensitive-field absence in login and profile responses
- P2 Root and self-operation protection

Medium complexity:

- P0 JWT format, signature, and payload tampering
- P1 brute-force thresholds and automatic account locking
- P1 successful-write idempotency and mutation verification
- P1 operation-level RBAC matrix
- P2 malformed JSON, content types, Unicode, and input-size boundaries

High or environment-sensitive complexity:

- P0 concurrent Refresh Token replay and atomic rotation
- P1 eventual audit-log delivery and integrity
- P2 complete TOTP lifecycle and secret rotation
- P2 credentialed CORS and CSRF behavior
- P2 production cookie-security profile

The implementation order is therefore:

1. Complete the low-complexity P0 Permission-delegation tests.
2. Complete low-complexity P1 user-state and password tests.
3. Complete low-complexity Refresh Token and response-leakage tests.
4. Add JWT tampering, brute-force, and successful idempotency tests.
5. Add concurrency, audit, TOTP, CORS, and production-profile tests.

Remaining Identity scope after P1:

- Framework API RBAC does not apply to unrelated MVC controllers
- User, Role, and Permission validation and conflict cases
- Full operation-level authorization metadata coverage

Current location:

```text
D:\python projects\AlcedoFrameworkTest\
  src/alcedo_testkit/
    http.py
    settings.py
    identity/
  tests/identity/
    test_auth.py
    test_authorization_metadata.py
    test_http_contract.py
    test_rbac.py
    test_token_lifecycle.py
```

### 2. MasterData API Tests

JuiceStore is now implemented as the reference vertical slice. The suite
contains five passing black-box tests covering:

- Create, query, update, enable, disable, and delete
- Duplicate code handling
- Optimistic concurrency conflicts
- Creator and updater information
- Public catalog visibility for enabled records only
- Invalid deletion while Enabled
- Idempotent deletion of an already deleted aggregate
- Ordinary-user RBAC enforcement outside the Identity module, including
  independent page-view and create permissions
- Eventual Create and Update audit-log delivery with operator information

Filtering, sorting, and pagination boundaries remain optional follow-up coverage
rather than prerequisites for the core MasterData lifecycle.

This single flow should validate the shared controller, application components,
repositories, unit of work, state machine, authorization, and event pipeline.

### 3. Package Consumer Tests

Required before products consume Framework primarily through NuGet:

- Pack publishable projects to a temporary feed
- Restore and build a separate consumer using only `PackageReference`
- Verify transitive dependencies
- Verify MVC Razor views and static web assets
- Verify Blazor module and route discovery
- Ensure test utilities are not accidentally packable

### 4. Minimal MVC And HTMX Tests

- Full-page Juice catalog response
- HTMX partial list response
- Search and pagination query preservation
- Quick-view partial
- Default and Sample-specific error pages
- MVC and DaisyUI static assets are available

### 5. Minimal Blazor E2E Test

Keep browser tests small. One golden path is sufficient initially:

```text
Login -> create Juice -> edit -> enable -> verify public catalog -> disable -> delete
```

## Tests Deferred To Consuming Projects

The following should be implemented while developing Hercules or another
business system rather than duplicated in Framework now:

- Product-specific aggregates and workflows
- Catalog, stock, order, payment, and customer/member behavior
- Customer-specific authentication schemes and policies
- Approval processes and organization-specific permissions
- Complex Blazor and MVC page workflows
- Business-specific Quartz jobs
- Third-party APIs, payment gateways, and production SMTP behavior
- Product-level performance, load, and acceptance tests

Framework should test its generic adapters and contracts, while consuming
projects test their actual business use cases.

## Recommended Implementation Order

1. Correct and verify the Problem Details media-type contract
2. Complete ordinary-user RBAC and cache-refresh acceptance tests
3. Complete locked, disabled, and password lifecycle acceptance tests
4. Add JuiceStore MasterData black-box API lifecycle tests
5. Add NuGet package-consumer smoke tests
6. Add minimal MVC/HTMX functional tests
7. Add one Blazor Playwright golden path

## Test Project Locations

```text
AlcedoFramework/
  Tests/
    Architecture/
      Alcedo.Architecture.Tests/

D:\python projects\AlcedoFrameworkTest\
  src/alcedo_testkit/
  tests/identity/

Future locations:
  AlcedoFramework/Tests/Packaging/Alcedo.PackageConsumer.Tests/
  AlcedoFramework/Tests/E2E/Alcedo.Sample.PlaywrightTests/
```

The Python API Testkit remains separate from the Framework repository so it
cannot accidentally couple tests to Framework services, repositories, EF Core,
or implementation types. This document records both the current implementation
state and the next testing milestones.
