# Security Review: CsrApi

**Date:** 2026-06-07
**Reviewer:** Claude (security review)
**Scope:** CsrApi (.NET 10 backend, LIFF + Backoffice + Shirt Order)
**Files Analyzed:** `Program.cs`, `LiffAuthMiddleware.cs`, `BackofficeEndpoints.cs`, `ShirtOrderEndpoints.cs`, `Repositories/StudentRepository.cs`, `Repositories/StaffRepository.cs`, `Services/EncryptionService.cs`, `Services/PhotoStorageService.cs`, `Services/SlipStorageService.cs`, `Services/RegistrationService.cs`, `Services/BackofficePolicy.cs`, `Services/LineProfileService.cs`, `Services/GoogleSheetsService.cs`, `appsettings.json`

---

## Summary

| # | Severity | Category | Issue |
|---|----------|----------|-------|
| 1 | Medium | Auth bypass | Forwarded Headers trust → rate limiter bypass |
| 2 | High (conditional) | Privilege escalation | Unauth dev role-switch creates Teacher accounts |
| 3 | Medium | Crypto | AES-CBC without MAC for PII at rest |
| 4 | Low | Data exposure | Internal `ex.Message` leaked in error responses |

**Non-findings (verified safe):** SQL injection (Dapper parameterized everywhere), file path traversal in photo/slip storage (SHA256 ownerKey + `Path.GetFileName`), XSS (JSON-only responses), deserialization (typed `JsonSerializerOptions.Web`), SSRF (`LineProfileService` host is hardcoded `api.line.me`, Google Sheets IDs are server-controlled), IDOR on `/api/me/*` (filtered by `lineUserId` from auth context), slip GET unauthenticated (by design, SHA256 in URL).

---

## Vuln 1: Forwarded Headers trust allows X-Forwarded-For spoofing to bypass rate limiting

* **Severity:** Medium
* **Category:** Authentication bypass / input validation
* **Location:** `Program.cs:27-34`, `Program.cs:121`, `Program.cs:59-66`
* **Confidence:** 8/10

### Description

`ForwardedHeadersOptions` is configured with `KnownIPNetworks.Clear()` and `KnownProxies.Clear()`, and `ForwardedHeaders = XForwardedFor | XForwardedProto`. The comment states the API runs behind nginx, but the configuration explicitly tells ASP.NET Core to trust forwarded headers from **any** source. The rate limiter at line 59-66 partitions by `context.Connection.RemoteIpAddress?.ToString()` — but `UseForwardedHeaders()` at line 121 **rewrites** `RemoteIpAddress` to the X-Forwarded-For value before the rate limiter runs.

### Exploit Scenario

```http
POST /api/register HTTP/1.1
X-Forwarded-For: 1.2.3.4
Authorization: Bearer <valid-token>
...
# Repeat with X-Forwarded-For: 1.2.3.5, 1.2.3.6, etc.
```

Attacker bypasses the 100 req/min per-IP limit, floods the registration endpoint or scrapes the API at unlimited volume.

### Recommendation

Remove `KnownIPNetworks.Clear()` and `KnownProxies.Clear()`. Explicitly add the nginx proxy IP/CIDR to `KnownProxies` / `KnownIPNetworks`. Do not trust forwarded headers from arbitrary sources.

---

## Vuln 2: Unauthenticated dev-only role switch can create Teacher accounts

* **Severity:** High (conditional on env misconfiguration)
* **Category:** Privilege escalation
* **Location:** `BackofficeEndpoints.cs:18-35`, `LiffAuthMiddleware.cs:33`
* **Confidence:** 7/10

### Description

`BackofficeEndpoints.MapBackofficeEndpoints` registers `MapPost("/api/backoffice/dev/switch-role", ...)` **unconditionally** inside an `if (app.Environment.IsDevelopment())` block. The handler is reachable because `LiffAuthMiddleware` line 33 explicitly **skips authentication for any path starting with `/api/backoffice/dev`**. The handler calls `staffRepo.UpsertStaffUserAsync` with `Role = req.Role` (no role allowlist at the handler level — any string accepted), creating a `Teacher` user tied to the attacker-supplied `LineUserId`.

If the production environment ever runs as `Development` (env var `ASPNETCORE_ENVIRONMENT=Development` misconfigured, a test deploy, a misconfigured staging env exposed publicly), any unauthenticated caller can create a Teacher and obtain full backoffice access (PII decryption, exports, staff management, status mutation).

### Exploit Scenario

```http
POST /api/backoffice/dev/switch-role HTTP/1.1
Content-Type: application/json

{"lineUserId":"U_ATTACKER_LINE_ID","role":"Teacher"}
```

Attacker registers as Teacher. On next login with that LINE ID they receive `StaffRole=Teacher` and full access to all `/api/backoffice/*` endpoints — exposing decrypted student names, phones, photos, PII of all parents.

### Recommendation

Add explicit role allowlist in the handler (`if (req.Role not in ["Teacher","ParentNetworkStaff"]) reject`). Better: gate behind a separate config flag (`Dev:Enabled` + secret) and remove the path-skip from LiffAuthMiddleware in production builds. Best: delete the endpoint entirely using `#if DEBUG` conditional compilation.

---

## Vuln 3: AES-CBC without authentication for PII at rest

* **Severity:** Medium
* **Category:** Cryptography
* **Location:** `Services/EncryptionService.cs:29-72`
* **Confidence:** 8/10

### Description

`EncryptionService.Encrypt` / `Decrypt` use AES-CBC with a random IV but **no authentication tag** (no GCM, no HMAC). A padding-oracle / bit-flipping attack on stored ciphertext is theoretically possible, but more concretely: any database/SQL write-level attacker who can flip ciphertext bytes can cause `Decrypt` to either return garbage or, more importantly, an attacker controlling the IV prefix in storage can swap ciphertext blocks across records to swap names/phones between records. CBC malleability is a well-known issue.

### Exploit Scenario

An attacker with write access to the SQLite database (e.g., via SQL injection in some other service, malicious backup restore, container escape) can swap `EncryptedName` between two student records by copying the IV + ciphertext block from row A into row B. After swap, the decrypted name for student B is now student A's name. Without authentication, the API cannot detect the swap.

Additionally, AES-CBC with no integrity also fails the "encrypt-then-MAC" or "AEAD" recommendation for at-rest PII (PDPA-relevant data here).

### Recommendation

Switch to AES-GCM (`AesGcm`) or use AES-CBC + HMAC-SHA256 encrypt-then-MAC. Rotate the existing ciphertext on next update or re-encrypt all rows under the new scheme.

---

## Vuln 4: Internal exception messages leaked in error responses

* **Severity:** Low
* **Category:** Data exposure
* **Location:** `Repositories/StudentRepository.cs:154, 175, 189, 211, 260, 327, 387, 443, 474, 535, 555`, `Program.cs:217`
* **Confidence:** 7/10

### Description

Repositories return `AppError.Internal($"Database error: {ex.Message}")`. The `/api/students` POST handler maps this to `Results.BadRequest(err.Message)` for status 400 (line 217), leaking the underlying Dapper/SQLite exception text — which can include column names, table names, and constraint details. Similar pattern in photo/slip storage services (`PhotoStorageService.cs:101, 131`, `SlipStorageService.cs:97, 127`) where `ex.Message` flows into `AppError.Internal`.

### Exploit Scenario

Attacker submits crafted inputs that trigger constraint violations or Dapper type errors. The 400 response leaks internal column names, table names, and data types, aiding further attacks.

### Recommendation

Sanitize error messages before returning to clients. Log `ex.Message` server-side; return a generic message like "Invalid request" to the client for all 4xx responses.
