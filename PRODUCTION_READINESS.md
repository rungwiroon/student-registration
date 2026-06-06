# Production Readiness Assessment

**Project:** Student Registration System  
**Assessed:** 2026-04-27  
**Status:** NOT READY for production — multiple critical and high-severity issues found.

---

## Critical (Fix Before Deploy)

### 1. Placeholder/Default Secrets in Configuration — FIXED
Placeholder secrets were present in configuration files tracked by Git.

| File | Line | Issue | Status |
|------|------|-------|--------|
| `CsrApi/appsettings.json` | 15 | `EncryptionKey` placeholder | Emptied |
| `CsrApi/appsettings.json` | 13-14 | DB password placeholder in connection string | Emptied |
| `docker-compose.yml` | 12 | DB password fallback in compose | Removed fallback, now uses `${CONNECTION_STRING}` |
| `docker-compose.yml` | 13 | `ENCRYPTION_KEY` fallback exposes default key | Removed fallback, now uses `${ENCRYPTION_KEY}` |
| `CsrApi/Services/EncryptionService.cs` | 21 | Hardcoded fallback key if config missing | Now throws `InvalidOperationException` |

**What was changed:**
- `appsettings.json`: `EncryptionKey` and `DefaultConnection` set to empty strings.
- `docker-compose.yml`: Removed `${VAR:-default}` fallbacks for `ConnectionStrings__DefaultConnection`, `EncryptionKey`, and `Line__LiffChannelId`. Now reads directly from env vars.
- `EncryptionService`: Removed fallback key. Constructor now throws if `EncryptionKey` is missing.
- `.env.example`: Updated with clear placeholder instructions and added `CONNECTION_STRING`.

**Deploy note:** Ensure `CONNECTION_STRING`, `ENCRYPTION_KEY`, and `LINE_LIFF_CHANNEL_ID` are set in your environment before starting containers.

---

### 2. Empty `.env` File Tracked in Git — FIXED
The `.env` file was already untracked (present in `.gitignore`). Verified via `git ls-files .env` — no output.

---

### 3. No CORS Configuration — FIXED
Added CORS policy in `Program.cs`.
- Development: permissive (`AllowAnyOrigin`).
- Production: restricts to `Cors:AllowedOrigins` config. Set via env var or config.

```csharp
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment()) { ... permissive ... }
    else { ... WithOrigins(allowedOrigins) ... }
});
app.UseCors("Production");
```

**Deploy note:** Configure `Cors:AllowedOrigins` in production to your frontend domain(s).

---

### 4. No Rate Limiting — FIXED
Added global fixed-window rate limiter in `Program.cs`. Limits to 100 requests per minute per IP. Returns 429 when exceeded.

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

---

### 5. No HTTPS / Security Headers — PARTIALLY FIXED
Added to API middleware pipeline:
- `UseHsts()` in non-development environments.
- Security headers middleware injecting `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`.

`UseHttpsRedirection` remains conditional on `Http:UseHttpsRedirection` config (disabled by default in Docker since TLS terminates at reverse proxy).

**Still needed:** Add same security headers to `nginx.conf` for defense in depth.

---

### 6. Missing Health Check Endpoint — FIXED
Registered `AddHealthChecks()` and mapped `/health` endpoint in `Program.cs`.

`LiffAuthMiddleware` already skips auth for `/health`, so no auth changes needed.

---

## High (Fix Soon)

### 7. Weak LINE Token Verification
`LiffAuthMiddleware.cs:78` verifies only `client_id`. It does **not** check:
- `expires_in` or token expiry
- Whether the token has been revoked

A stolen or cached token may continue to work past its intended lifetime.

**Fix:** Parse `expires_in` from the LINE verify response and cache validation results with TTL. Also consider verifying the token against LINE's profile endpoint as a secondary check.

---

### 8. No Global Request Size Limit — FIXED
Added Kestrel max request body size limit of 10 MB in `Program.cs`.

**Still needed:** Add nginx `client_max_body_size 10m;` to `nginx.conf`.

---

### 9. No Centralized Exception Handling — FIXED
Added `UseExceptionHandler` middleware at start of pipeline in `Program.cs`. Returns generic 500 JSON without stack traces.

```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { Error = "An unexpected error occurred." });
    });
});
```

---

### 10. Dev Endpoint Ships in Production Binaries
`BackofficeEndpoints.cs:19-35` contains `/api/backoffice/dev/switch-role` which is gated by `IsDevelopment()`. However, the endpoint code is still present in production binaries. If the environment check is bypassed (misconfiguration), the endpoint is exposed.

**Fix:** Move dev-only endpoints to a separate file/class that is only compiled in debug builds, or use `#if DEBUG`.

---

## Medium

### 11. SQLite in Docker Volume — Scaling & Backup Risk
SQLite is fine for small deployments, but:
- The database lives on a single Docker volume (`app_data`).
- No automated backup strategy.
- `InitializeDatabaseAsync` runs on every startup, which is unnecessary overhead.
- Database migrations are handled via raw SQL in `InitializeDatabaseAsync` — no versioning.
- Horizontal scaling (multiple API replicas) is not possible with SQLite.

**Fix:**
- For production, consider PostgreSQL or MySQL if scale is anticipated.
- If staying with SQLite, add a backup cron job and document the single-replica constraint.

---

### 12. Nginx Missing Timeout and Buffer Config
`nginx.conf` proxies to the API but does not set:
- `proxy_read_timeout`
- `proxy_connect_timeout`
- `client_max_body_size`

**Fix:** Add timeout and buffer directives to prevent hung connections and oversized uploads.

---

### 13. Docker Compose Missing Resilience
`docker-compose.yml` has no:
- `restart: unless-stopped`
- Memory or CPU limits
- Health checks for either service
- `depends_on` with condition for API readiness before frontend starts

**Fix:** Add `restart`, `deploy.resources`, and `healthcheck` blocks.

---

### 14. `AllowedHosts: "*"` Unnecessary Risk
`appsettings.json:8` sets `AllowedHosts: "*"`. Combined with missing CORS, this widens the attack surface for host-header attacks.

**Fix:** Set `AllowedHosts` to the actual production domain.

---

## Low / Code Quality

| File | Line | Issue |
|------|------|-------|
| `CsrApi/Services/RegistrationService.cs` | 354-357 | Duplicate `NewNo > 50` validation check |
| `CsrApi/BackofficeEndpoints.cs` | 30 | `.Result` used on async call in dev endpoint — blocks thread |
| `CsrApi/BackofficeEndpoints.cs` | 222-224 | `ParentNetworkStaff` receives `InternalNote` in response despite policy hiding it; value is present but should be excluded |
| `CsrApi/Middleware/LiffAuthMiddleware.cs` | 89-93, 110-113 | Empty `catch` blocks swallow exceptions; at minimum log them |

---

## Recommendations Summary

### Critical — Done in This Pass
| # | Item | Status |
|---|------|--------|
| 1 | Remove placeholder secrets from source code | Done — `appsettings.json` and `docker-compose.yml` cleared |
| 2 | Remove encryption key fallback | Done — `EncryptionService` now throws if missing |
| 3 | Configure CORS | Done — added `AddCors` + `UseCors`, dev permissive, production restricted |
| 4 | Add rate limiting | Done — global 100 req/min per IP |
| 5 | Add security headers / HSTS | Done — middleware added |
| 6 | Register `/health` endpoint | Done — `AddHealthChecks` + `MapHealthChecks` |
| 7 | Add exception handling middleware | Done — generic 500 responses |
| 8 | Add request size limits | Done — 10 MB Kestrel limit |

### High — Remaining
| # | Item | Status |
|---|------|--------|
| 9 | Strengthen LINE token verification with expiry checks | Open |
| 10 | Remove or guard dev-only endpoints more strictly | Open |

### Medium — Remaining
| # | Item | Status |
|---|------|--------|
| 11 | Add nginx security headers (defense in depth) | Open |
| 12 | Add nginx `client_max_body_size` | Open |
| 13 | Add Docker Compose `restart`, `healthcheck`, resource limits | Open |
| 14 | Set `AllowedHosts` to production domain | Open |
| 15 | Evaluate SQLite → PostgreSQL for scale | Open |

### Consider for Scale
| # | Item | Status |
|---|------|--------|
| 16 | Add structured logging (Serilog) | Open |
| 17 | Add metrics and monitoring | Open |
| 18 | Database backup strategy | Open |
| 19 | CI/CD with security scanning | Open |

---
curl -X POST https://your-domain/api/bootstrap \
  -H "Content-Type: application/json" \
  -d '{"secretToken":"<your-secret>","lineUserId":"Uxxxxxxx","name":"ชื่อครู"}'