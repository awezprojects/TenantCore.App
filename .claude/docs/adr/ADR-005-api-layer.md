# ADR-005 — API Layer: Controllers, Middleware & Authorization

**Repo:** TenantCore.App  
**Status:** Active  
**Layer:** `TenantCore.Api`  
**Path:** `src/TenantCore.Api/`

---

## Decision

The API layer is the composition root and entry point. Controllers are thin — they translate HTTP requests to MediatR commands/queries and return HTTP responses. Authorization is handled via custom policies and a per-clinic role handler. The middleware pipeline has a fixed, deliberate order.

---

## Folder Structure

```
TenantCore.Api/
├── Controllers/
│   ├── ClinicControllerBase.cs        # Base class for ALL clinic-scoped controllers
│   ├── AuthController.cs
│   ├── RoleController.cs
│   ├── PatientsController.cs
│   ├── PrescriptionsController.cs
│   ├── MedicinesController.cs
│   ├── MedicineTypesController.cs
│   ├── DoctorProfileController.cs
│   ├── ClinicController.cs
│   ├── ApplicationController.cs
│   └── ... (20 controllers total)
├── Middleware/
│   ├── CorrelationIdMiddleware.cs     # Adds X-Correlation-Id to every request
│   ├── ExceptionHandlingMiddleware.cs # Maps exceptions to HTTP problem responses
│   └── ClinicContextMiddleware.cs     # Validates X-Application-Id header
├── Authorization/
│   ├── ClinicRoleRequirement.cs       # Custom authorization requirement
│   └── ClinicRoleAuthorizationHandler.cs  # Custom handler for per-clinic roles
├── Extensions/
│   └── ServiceCollectionExtensions.cs # JWT authentication setup
└── Program.cs                         # App bootstrap and middleware pipeline
```

---

## Controller Pattern: `ClinicControllerBase`

**Every controller that operates within a clinic context inherits from `ClinicControllerBase`** — never from plain `ControllerBase`.

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public abstract class ClinicControllerBase : ControllerBase
{
    protected readonly IMediator _mediator;

    protected ClinicControllerBase(IMediator mediator)
    {
        _mediator = mediator;
    }

    protected Guid GetApplicationId()
    {
        // Returns the validated clinic/application ID from HttpContext.Items
        // Populated by ClinicContextMiddleware
    }

    protected string GetApplicationName()
    {
        // Returns the clinic name from HttpContext.Items
    }
}
```

**Rules for controllers:**

1. Only `IMediator` is injected into controller constructors — no repositories, no services
2. Action methods are one-liners (dispatch to MediatR, return result):
   ```csharp
   [HttpGet("{id}")]
   public async Task<ActionResult<PatientDto>> GetById(Guid id)
   {
       var result = await _mediator.Send(new GetPatientByIdQuery
       {
           PatientId = id,
           ApplicationId = GetApplicationId()
       });
       return result is null ? NotFound() : Ok(result);
   }
   ```
3. Always return `ActionResult<T>` or `IActionResult` — never raw objects
4. HTTP verb attributes: `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, `[HttpPatch]`
5. Route parameters go in the URL, complex request bodies come from `[FromBody]`

### Controller Authorization

Use `[Authorize]` attributes at class or method level with policies from `AuthorizationConstants.AuthPolicies`:

```csharp
[Authorize(Policy = AuthPolicies.RequireClinicAdmin)]
[HttpPost]
public async Task<ActionResult<Guid>> Create([FromBody] CreatePatientRequest request) { ... }
```

Available policies (defined in `TenantCore.Shared/Authorization/AuthorizationConstants.cs`):
- `RequireAuthenticated` — any valid JWT
- `RequireClinicAdmin` — `ClinicAdmin` role for the current clinic
- `RequireReception` — `Reception` role for the current clinic
- `RequireClinical` — `Clinical` role for the current clinic

---

## Middleware Pipeline

The middleware pipeline in `Program.cs` has a **fixed order** that must not be changed:

```csharp
// 1. Correlation ID — must be FIRST (all subsequent logging uses it)
app.UseMiddleware<CorrelationIdMiddleware>();

// 2. Global exception handler — SECOND (wraps everything in error handling)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 3. Request logging — THIRD (logs requests with correlation ID attached)
app.UseSerilogRequestLogging();

// 4. Swagger UI — development only
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

// 5. Static files for Blazor WebAssembly
app.UseBlazorFrameworkFiles();
app.UseStaticFiles(new StaticFileOptions { ... });

// 6. CORS — before auth
app.UseCors("AllowAll");

// 7. Authentication — validates JWT bearer tokens
app.UseAuthentication();

// 8. Clinic context — AFTER authentication (reads validated JWT claims)
app.UseMiddleware<ClinicContextMiddleware>();

// 9. Authorization — AFTER clinic context (per-clinic role check needs the context)
app.UseAuthorization();

// 10. Endpoints
app.MapControllers();
app.MapHealthChecks("/health");
app.MapFallbackToFile("index.html");  // SPA fallback for Blazor
```

**Why this order matters:**
- `CorrelationIdMiddleware` must be first so every log line has a correlation ID
- `ExceptionHandlingMiddleware` must wrap everything so unhandled exceptions return problem JSON
- `ClinicContextMiddleware` must be after `UseAuthentication()` — it reads JWT claims
- `UseAuthorization()` must be after `ClinicContextMiddleware()` — the custom handler reads HttpContext.Items set by that middleware

---

## Custom Middleware Details

### `CorrelationIdMiddleware`
- Reads `X-Correlation-Id` header from the request (or generates a new GUID)
- Adds it to the response headers
- Pushes it into Serilog's `LogContext` so every log line includes it

### `ExceptionHandlingMiddleware`
- Catches all unhandled exceptions
- Maps them to RFC 7807 `ProblemDetails` JSON responses:
  - `ValidationException` → 400 Bad Request
  - `EntityNotFoundException` → 404 Not Found
  - Everything else → 500 Internal Server Error
- Logs the exception with the correlation ID

### `ClinicContextMiddleware`
- Reads `X-Application-Id` header from the request
- Validates it against the `app_ids` JWT claim (user must be a member of the clinic)
- Stores the validated application ID and name in `HttpContext.Items`
- Returns `403 Forbidden` if the header is missing or the user doesn't have access to that clinic

---

## JWT Authentication Setup

JWT is configured in `Extensions/ServiceCollectionExtensions.cs` via `AddJwtAuthentication(config)`:

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = true,
            ValidIssuer = "ApplicationAuth",
            ValidateAudience = true,
            ValidAudience = "ApplicationAuthUsers",
            ClockSkew = TimeSpan.Zero
        };
    });
```

**Config keys (appsettings.json):**
```json
{
  "Jwt": {
    "Secret": "...",
    "Issuer": "ApplicationAuth",
    "Audience": "ApplicationAuthUsers"
  }
}
```

The secret must match the one used by TenantCore.Auth to sign tokens.

---

## Custom Authorization: Per-Clinic Role

`ClinicRoleAuthorizationHandler` enforces that the user has a specific role in the **current clinic** (not just globally):

```csharp
// ClinicRoleRequirement specifies the required role name
// ClinicRoleAuthorizationHandler reads:
//   - HttpContext.Items["ApplicationId"] (set by ClinicContextMiddleware)
//   - JWT claim "app_roles" (JSON array from TenantCore.Auth)
// Returns Succeed only if user has the required role for that specific clinic
```

---

## Configuration Layering — `appsettings.json` vs `appsettings.Local.json`

`Program.cs` loads configuration in this order (later sources override earlier ones for the same key):

```csharp
// 1. appsettings.json                 — checked in, safe defaults / empty placeholders only
// 2. appsettings.{Environment}.json   — checked in (e.g. appsettings.Development.json)
// 3. appsettings.Local.json           — gitignored, loaded explicitly, LAST — wins
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
```

**Rule — whenever a new configuration section or key is added (a new connection string, a new external service's base URL, any secret):**

1. Add the key to `appsettings.json` with an **empty string or clearly-fake placeholder value** — this file is checked into source control and must never contain a real secret or working connection string.
2. Add the **same key** to `appsettings.Local.json` with the **actual real value** for local development/testing. This file is gitignored — real secrets live here, never in `appsettings.json` or `appsettings.Development.json`.
3. If the key needs an environment-specific but still non-secret default (e.g. a different `AuthApi:BaseUrl` for Development vs. Production), add it to `appsettings.{Environment}.json` instead of `appsettings.Local.json` — reserve `appsettings.Local.json` for values that are genuinely per-developer or secret.

**Why this matters:** Visual Studio (and `dotnet run`) load `appsettings.Local.json` last regardless of environment, so it is the effective source of truth for local runs whenever a key is defined there. A key that only exists in `appsettings.json` with an empty value will silently stay empty locally until someone adds the real value to `appsettings.Local.json` — this is the expected, intentional behavior, not a bug to route around by putting real values into `appsettings.json`.

**What NOT to do:**
- Do NOT put a real connection string, API key, or secret directly into `appsettings.json` or `appsettings.Development.json` — both are checked in
- Do NOT add a new config section to only one of the two files — `appsettings.json` documents the shape (with placeholders) for every environment; `appsettings.Local.json` supplies the real local value
- Do NOT rely on `appsettings.Development.json` for secrets — it is also checked in

---

## Health Check

```
GET /health
```

Checks:
- SQL Server connectivity (`AspNetCore.HealthChecks.SqlServer`)

Returns 200 OK (healthy) or 503 Service Unavailable.

---

## Step-by-Step: Adding a New Controller

1. Create `Controllers/{Feature}Controller.cs`
2. Inherit from `ClinicControllerBase` (always, unless it's a global endpoint with no clinic context)
3. Add `[ApiController]` and `[Route("api/[controller]")]` attributes
4. Add `[Authorize(Policy = ...)]` at class level with the minimum policy for the feature
5. Inject only `IMediator` via constructor
6. For each endpoint:
   - Create a query or command in the Application layer (ADR-003)
   - Dispatch via `_mediator.Send(new MyCommand { ... ApplicationId = GetApplicationId() })`
   - Return appropriate `ActionResult<T>`

---

## What NOT to Do

- Do NOT inject repositories or services directly into controllers
- Do NOT put validation logic in controllers — that belongs in FluentValidation validators
- Do NOT change the middleware pipeline order
- Do NOT use `[FromHeader]` to read `X-Application-Id` in controllers — use `GetApplicationId()` from the base class
- Do NOT add a controller that doesn't inherit `ClinicControllerBase` unless it's a truly global endpoint (e.g., health check)
