# Android LIFF Init Hang — Investigation Report

**Date:** 2026-05-02  
**App:** student-registration (csr-frontend)  
**Platform:** Android LINE WebView  
**Reference working app:** it-support/it-support-system/liff-app

---

## 1. Problem

When opening the app from LINE on **Android**, the LINE "เข้าสู่ระบบแล้ว" (already logged in) modal appears. After clicking "ดำเนินการต่อ" (continue), the Dashboard never loads student info. On **iPhone** the same flow works instantly with no modal.

### Symptoms
- `liff.init()` hangs for 10–60 seconds, then times out
- Android WebView auto-refreshes the page after ~15 seconds while `init()` is still running
- After refresh, `liff.init()` tries again with the **same stale OAuth `code`** in URL
- This creates an infinite loop: hang → refresh → stale code → hang → refresh
- Debug panel shows `Failed to fetch` or timeout errors repeatedly

---

## 2. Environment

| Component | Version |
|-----------|---------|
| `@line/liff` (original) | ^2.28.0 (npm) |
| CDN SDK (current) | edge/2 |
| Vue | ^3.5.30 |
| Vite | ^8.0.1 |
| Android | 16 (SM-A566B) |
| User Agent | `Mozilla/5.0 (Linux; Android 16; SM-A566B ...)` |
| LIFF ID | `2009916202-YGxGSKdi` |
| Channel ID | `2009916202` |

---

## 3. Root Cause Analysis

### 3.1 Stale OAuth Code (Primary)
LINE OAuth `code` parameter in the URL is **single-use**. The flow:

1. User opens app → `liff.init()` starts → LINE exchanges `code` for access token internally
2. Page refreshes (WebView auto-kill or manual reload) **while `init()` is still running**
3. The `code` has already been consumed by the first attempt
4. Second `liff.init()` call sees same `code` in URL → tries to exchange it again → LINE rejects → SDK Promise **hangs indefinitely**
5. Android WebView detects "frozen" page (no console output, no DOM changes) → auto-refreshes after ~15s
6. Loop repeats forever

**Evidence:** Every log shows a different `code` value, but within each session the same code is retried multiple times after refresh.

### 3.2 Android WebView Auto-Refresh
Android's custom WebView implementation kills pages that appear frozen. `liff.init()` can legitimately take 5–15 seconds on Android (native bridge setup). Without periodic console output or DOM updates, WebView refreshes the page mid-init.

**Evidence:** Logs show consistent ~6–17 second intervals between "init start" and "PAGE REFRESHED".

### 3.3 npm Module vs WebView-Injected SDK Conflict (Secondary)
Our app originally bundled `@line/liff` via npm (`import liff from '@line/liff'`). On Android WebView, LINE injects `window.liff` globally. The npm module and injected SDK can conflict, causing `init()` to hang with `Failed to fetch`.

**Evidence:** Working `it-support` app uses CDN `window.liff` only (no npm import). Switching to CDN reduced `Failed to fetch` frequency.

### 3.4 Pre-Init Check Caused False Skip
Early optimization: tried `window.liff.isLoggedIn()` before `init()` to skip init if SDK "already initialized". On corrupted half-initialized state, `isLoggedIn()` didn't throw but `getAccessToken()` returned null and `isInClient()` threw. This masked the real problem.

---

## 4. Attempted Solutions (Chronological)

### 4.1 Retry `getAccessToken()` with delays
- **What:** Android-only retry loop 5×200ms → 10×300ms for `getAccessToken()`
- **Result:** Helped on some devices but not the root cause. Token was null because `init()` never completed.
- **Status:** Still present in code as defensive measure.

### 4.2 Redirect loop guard (`sessionStorage` counter)
- **What:** Count `liff.login()` redirects, max 3 then throw error.
- **Result:** Prevented infinite redirect but didn't fix the hang.
- **Status:** Still present.

### 4.3 Remove custom `redirectUri`
- **What:** Removed `redirectUri` parameter from `liff.init()` and `liff.login()`.
- **Result:** Eliminated 400 Bad Request errors.
- **Status:** Permanent fix.

### 4.4 Increase timeout (5s → 20s → 45s → 60s)
- **What:** Extended `Promise.race` timeout for `liff.init()`.
- **Result:** `init()` still timed out at 60s. Problem was stale code, not slow init.
- **Status:** Reverted to 10s.

### 4.5 Pre-delay before `init()` (1s → 3s)
- **What:** Fixed delay for Android native bridge to settle.
- **Result:** Made things slower without fixing root cause.
- **Status:** Removed.

### 4.6 Prefer `window.liff` over npm module
- **What:** Added `getLiff()` helper checking `window.liff` first, falling back to npm.
- **Result:** Reduced `Failed to fetch` but `init()` still hung on stale code.
- **Status:** Evolved into CDN-only approach.

### 4.7 CDN-only approach (removed npm `@line/liff`)
- **What:** Added `<script src="https://static.line-scdn.net/liff/edge/2/sdk.js">` to `index.html`, removed npm dependency.
- **Result:** Eliminated npm/WebView conflict. `Failed to fetch` became rare.
- **Status:** Permanent fix.

### 4.8 Debug panel with sessionStorage persistence
- **What:** Intercept `console.log/error/warn`, persist to `sessionStorage`, show/hide panel with "D" button, "Copy All" to clipboard.
- **Result:** Essential for diagnosing Android issues. Logs survive page refresh.
- **Status:** Permanent feature.

### 4.9 `watch(isReady)` + `visibilitychange` auto-retry
- **What:** Dashboard watches `isReady` ref and retries init when page becomes visible.
- **Result:** `visibilitychange` caused infinite retry loops. Removed.
- **Status:** `watch(isReady)` kept; `visibilitychange` removed.

### 4.10 Stale code detection (`sessionStorage` tracking)
- **What:** Track last processed `code` in `sessionStorage`. If current URL contains same code → remove from URL before `init()`.
- **Result:** Critical fix. Prevents SDK from hanging on consumed code.
- **Status:** Permanent.

### 4.11 Keepalive timer during `init()`
- **What:** `setInterval` every 500ms logging "keepalive tick N" while `init()` runs.
- **Result:** Prevents Android WebView from thinking page is frozen and auto-refreshing.
- **Status:** Permanent. Evidence in logs: 30+ ticks over 14s with no auto-refresh.

### 4.12 Pre-init check removal
- **What:** Originally skipped `init()` if `isLoggedIn()` didn't throw. Changed to always call `init()` regardless.
- **Result:** Eliminated half-initialized state bug.
- **Status:** Permanent.

### 4.13 `Failed to fetch` retry with URL cleanup
- **What:** Two distinct error recovery paths for callback init failures:
  1. **"Failed to fetch"** → clean URL, mark code processed, retry `init()` once without callback params (10s timeout)
  2. **Other init errors** (timeout, network, etc.) → mark code processed, clean URL, **continue to post-init SDK checks anyway**. The SDK is sometimes usable even after `init()` throws.
- **Result:** Not yet tested in production.
- **Status:** Removed. Simplification proved unnecessary.

### 4.14 Fix stale `initLiff` references in other views (2026-05-03)
- **What:** After removing `initLiff()` from `useLiff.js`, other views (`Register.vue`, `ClassList.vue`, `Contacts.vue`, `BackofficeLayout.vue`, and all backoffice views) still called the now-removed function, causing `TypeError: i is not a function` at runtime.
- **Fix:** Replaced `await initLiff()` with token check + redirect to `/liff-entry.html` in all 10 affected files.
- **Result:** All views now load correctly without LIFF init errors.
- **Status:** Permanent fix.

### 4.15 Cache-control meta tags
- **What:** Added `Cache-Control: no-cache` meta tags to prevent old JS bundle from being cached.
- **Result:** Ensures latest code is loaded after deployment.
- **Status:** Permanent.

### 4.16 Conditional timeout based on callback detection
- **What:** `getCallbackParams()` checks URL for `code`+`state`+`liffClientId`. If present, `liff.init()` timeout = 60s. Otherwise 10s.
- **Result:** Critical fix. First callback attempt needs 10–30s for internal OAuth exchange. 10s timeout was killing it mid-process. 60s allows SDK to finish.
- **Status:** Permanent.

### 4.17 Restored `getCallbackParams()` after simplification
- **What:** Removed during over-simplification (test page had no callback detection). Added back because Vue app must distinguish callback path from normal path for timeout selection.
- **Result:** Necessary for conditional timeout.
- **Status:** Permanent.

---

## 5. What Was Fixed (2026-05-02)

### The Fix: Separate HTML Entry Page
Instead of doing LIFF init inside the Vue app (where `liff.init()` hung), moved LIFF init to a standalone simple HTML page (`/liff-entry.html`). The Vue app never calls `liff.init()` — it reads the token from `localStorage`.

**Flow:**
1. User opens `/dashboard` from LINE → callback params in URL
2. `Dashboard.vue` detects callback params → redirects to `/liff-entry.html` + query string
3. `/liff-entry.html` (simple HTML, no Vue) runs `liff.init()` with 60s timeout
4. On success: stores token in `localStorage`, redirects to `/dashboard`
5. `Dashboard.vue` reads token from `localStorage`, calls `/api/me`

**Result:** Dashboard loads successfully. `/api/me` returns 200. Student data displays.

### Why This Fixed It
The standalone HTML page (same pattern as `test-liff.html`) handles LIFF init without Vue reactivity, component lifecycle, or composable state getting in the way. The SDK has a clean environment.

### What Is NOT Fixed
The LINE "เข้าสู่ระบบแล้ว" (already logged in) modal still appears on every visit. This is LINE SDK behavior — the modal is shown by LINE during `liff.init()` session refresh. Not controllable from our code. It is annoying but functional.

---

## 6. Current Code State

### `/public/liff-entry.html`
1. Static HTML page (copied to `dist/` by Vite)
2. Loads LIFF CDN SDK
3. Detects callback params in URL
4. Calls `liff.init({ liffId })` with 60s timeout (callback) / 10s (normal)
5. If not logged in → `liff.login()` → redirect back → init again
6. On success → stores in `localStorage`:
   - `liff_access_token`
   - `liff_user_id`
   - `liff_display_name`
   - `liff_is_in_client`
7. Redirects to `/dashboard`
8. Shows spinner + retry button on error

### `useLiff.js` — Key behaviors
1. No `initLiff()` — LIFF init is done in entry page
2. `getAccessToken()` → reads from `localStorage.getItem('liff_access_token')`
3. `profile` → reads from `localStorage` (`liff_user_id`, `liff_display_name`)
4. `closeWindow()` → optional helper
5. `login()` → redirects to `/liff-entry.html`
6. `LIFF_COMPOSABLE_VERSION` exported for debugging

### `Dashboard.vue` — Key behaviors
1. On mount: detects callback params → redirects to `/liff-entry.html`
2. On mount: no callback params but no token → redirects to `/liff-entry.html`
3. On mount: token present → calls `/api/me` directly
4. No LIFF init in Vue app
5. Error state shows retry button → redirects to `/liff-entry.html`

### `index.html` — Key changes
1. `<script src="https://static.line-scdn.net/liff/edge/2/sdk.js">` before app bundle (for `closeWindow()`)
2. Cache-control meta tags

---

## 7. What Works

| Approach | Effectiveness |
|----------|--------------|
| **Simple HTML entry page (`/liff-entry.html`)** | **Critical fix. Isolates LIFF init from Vue complexity. Proven working in production.** |
| CDN-only LIFF | Eliminates npm/WebView conflict |
| Debug panel | Essential for mobile diagnosis |
| `redirectUri` removal | Eliminated 400 Bad Request |

## 8. What Did Not Work

| Approach | Why It Failed |
|----------|---------------|
| Increasing timeout to 60s | Root cause was Vue wrapper, not slow init |
| Pre-delay before init | Just made UX slower |
| `visibilitychange` auto-retry | Created infinite retry loops |
| Pre-init check skipping init | SDK was in half-initialized corrupted state |
| Prefer `window.liff` with npm fallback | Fallback still triggered conflicts |
| Mock `line-code:` token fallback | Backend requires real LINE token verification |
| `withLoginOnExternalBrowser: false` | Didn't fix Android-specific issues |
| **No timeout on callback `init()`** | **Critical bug — caused infinite freeze when OAuth exchange hung. Fixed with conditional timeout: 60s for callback path, 10s for normal.** |
| **Complex wrappers around `init()`** | **Stale code detection, keepalive, pre-check, multiple recovery paths — all unnecessary. The test page proved a simple `init()` call works perfectly.** |

---

## 9. Key Learnings

1. **Complexity was the actual bug.** The test page (50 lines of simple JS) worked perfectly while the Vue composable (200+ lines with defensive code) failed. Every "defensive" feature we added (stale code detection, keepalive, pre-check, retry paths) potentially introduced new failure modes.
2. **LINE OAuth `code` is single-use.** Any page refresh after `liff.init()` starts can consume it. A simple timeout on `init()` handles this gracefully — no need for `sessionStorage` tracking.
3. **SDK continues OAuth exchange in background even after JS Promise rejects.** If `init()` times out during callback processing, the SDK may still complete the token exchange internally. A retry can succeed instantly because the SDK already finished. This is why a longer timeout (60s) on the callback path is critical — it prevents falsely rejecting a Promise that would have resolved.
4. **Android WebView auto-refreshes "frozen" pages.** But `liff.init()` on CDN SDK resolves in ~1s when opened from LINE. The 10-60s hangs were caused by corrupted SDK state from our complex retry logic, not by slow init.
5. **Always call `liff.init()` regardless of state.** Never trust `isLoggedIn()` or `getAccessToken()` to determine if init is needed. The SDK can be in a corrupted half-initialized state.
6. **CDN `window.liff` > npm `@line/liff` on Android WebView.** The WebView injects its own SDK. Bundling npm module causes conflicts.
7. **Cache invalidation is critical on mobile.** Browsers (especially WebView) aggressively cache JS bundles. Meta tags or query string cache-busting required.
8. **Test page first, then integrate.** The test page proved the SDK works before we touched the Vue app. This isolation was essential for finding the real cause.
9. **Simple HTML entry page isolates LIFF init from Vue.** The final fix was not making the Vue composable work — it was bypassing it entirely. A standalone HTML page handles the fragile LIFF init, stores the token, and redirects to the Vue app. The Vue app never calls `liff.init()`.

---

## 10. Reference: Working App Patterns

### `it-support` app
The `it-support` app works on the same Android device because it:
1. Loads LIFF via CDN in `index.html`
2. Never imports `@line/liff` npm module
3. Always calls `window.liff.init({ liffId })` without pre-checks
4. Has a manual "Login to LINE" button for fallback
5. Has no auto-retry loops

### Test page (`/test-liff.html`)
Created a minimal standalone test page that:
- Loads the same CDN SDK
- Calls `init()` with simple 25s timeout
- Logs every step directly to the DOM
- Persists logs across refreshes
- Works perfectly on the same Android device

**Access:** `https://skn50-smte.com/test-liff.html` or `https://skn50-smte.com/test-liff.html?auto=1`

**What it proved:** The LIFF SDK, LIFF ID, and OAuth flow are all configured correctly. The problem was entirely in the Vue app's wrapper complexity.

---

## 11. Test Results

### 2026-05-02 — Standalone test page (`/test-liff.html`)

**Test 1:** Direct open from LINE (no callback params)  
**Result:** `PASS` — `init()` resolved in ~1s

```
22:00:16 [LOG] URL: https://skn50-smte.com/test-liff.html
22:00:16 [LOG] URL params: code=false state=false liffClientId=false
22:00:16 [INFO] Pre-check: not initialized (liffId is necessary for liff.init())
22:00:16 [LOG] Calling liff.init() with 25000ms timeout...
22:00:16 [INFO] liff.init() resolved
22:00:16 [LOG] isInClient=false
22:00:16 [LOG] isLoggedIn=true
22:00:16 [INFO] User IS logged in
22:00:16 [LOG] getAccessToken=present (eyJhbGciOiJIUzI1NiJ9...)
22:00:17 [INFO] getProfile OK: userId=U2cee785e13ef369fd1e013db105e9101 name=Kung 2Na
```

**Test 2:** Private browser with `?auto=1` (fresh login flow)  
**Result:** `PASS` — Login modal appeared, clicked continue, redirect back, `init()` resolved, profile loaded.

**Conclusion:** LIFF SDK, LIFF ID, OAuth configuration, and device are all working correctly. The problem was entirely in the Vue app's `useLiff.js` wrapper complexity.

### 2026-05-02 — Production with `/liff-entry.html` (Android)

**Test:** Open app from LINE → callback URL → `/dashboard` redirects to `/liff-entry.html` → init → redirect back → API call  
**Result:** `PASS`

```
23:12:30 [log] [Debug] URL: https://skn50-smte.com/dashboard?state=UHN0JAkn7yRP&liffClientId=...&code=Ij77b1sAU9ZT7CZ7sVcV
23:12:33 [log] [Debug] URL: https://skn50-smte.com/dashboard  ← after redirect from entry page
23:12:34 [log] [Dashboard] /api/me status= 200
23:12:34 [log] [Dashboard] /api/me data= {"student":true,"guardians":2}
23:12:34 [log] [Dashboard] load complete, studentData= true
```

**Conclusion:** Entry page approach works. Dashboard loads student data successfully. LINE modal still appears (LINE SDK behavior, not fixable).

---

## 12. Open Questions

1. **Can the LINE modal be suppressed?** No. The modal is shown by LINE SDK during `liff.init()`. Not controllable from client-side code. Server-side OAuth bypass would be required.
2. **Should we add a backend endpoint that accepts `code` directly and exchanges it server-side?** Good long-term idea for eliminating the modal and making the flow smoother. Not urgent since current flow works.
3. **Is `localStorage` token storage secure enough?** Token is short-lived (~12h). For production hardening, consider refreshing token periodically or implementing server-side OAuth.

---

## 13. Files Modified

| File | Key Changes |
|------|-------------|
| `csr-frontend/index.html` | Added LIFF CDN script, cache-control meta tags |
| `csr-frontend/package.json` | Removed `@line/liff` dependency |
| `csr-frontend/src/composables/useLiff.js` | **Simplified** — no `initLiff()`, reads token from `localStorage`, `login()` redirects to `/liff-entry.html` |
| `csr-frontend/src/views/Dashboard.vue` | No LIFF init. Detects callback params → redirects to `/liff-entry.html`. Reads token from storage for API calls. |
| `csr-frontend/src/views/Register.vue` | No `initLiff()`. Token check → redirect to `/liff-entry.html` if missing. |
| `csr-frontend/src/views/ClassList.vue` | No `initLiff()`. Token check → redirect to `/liff-entry.html` if missing. |
| `csr-frontend/src/views/Contacts.vue` | No `initLiff()`. Token check → redirect to `/liff-entry.html` if missing. |
| `csr-frontend/src/components/BackofficeLayout.vue` | No `initLiff()`. Token check → redirect to `/liff-entry.html` if missing. Backoffice uses same LINE token for `fetchCurrentUser(token)` — staff are identified by LINE userId. |
| `csr-frontend/src/views/backoffice/*.vue` | All backoffice views: no `initLiff()`. Token check → redirect to `/liff-entry.html` if missing. Same reason as above — APIs require LINE bearer token. |
| `csr-frontend/src/components/MainLayout.vue` | Added debug panel with sessionStorage persistence, version display |
| `csr-frontend/public/test-liff.html` | **New** — standalone minimal LIFF test page for Android diagnosis |
| `csr-frontend/public/liff-entry.html` | **New** — simple HTML entry page that handles LIFF init, stores token in `localStorage`, redirects to `/dashboard` |
