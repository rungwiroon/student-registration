// Remembers which page the user was heading to, so /liff-entry.html can send
// them back there after LINE login instead of always landing on /dashboard.
// The same key and validation rules are duplicated in public/liff-entry.html
// (a standalone page that cannot import this module).

export const POST_LOGIN_REDIRECT_KEY = 'csr_post_login_redirect';

const ENTRY_PATH = '/liff-entry.html';

/**
 * Returns the value only if it is a same-origin path ("/venue-vote?x=1").
 * Anything that could leave the site ("//evil.com", "https://...", "/\\evil")
 * is rejected, so the redirect parameter cannot be used as an open redirect.
 */
export function toSafeInternalPath(value) {
  if (typeof value !== 'string' || !/^\/(?![/\\])/.test(value)) return null;
  try {
    const url = new URL(value, window.location.origin);
    if (url.origin !== window.location.origin) return null;
    if (url.pathname === ENTRY_PATH) return null;
    return url.pathname + url.search;
  } catch {
    return null;
  }
}

/**
 * Reads the target page from a query object: either our own `redirect`
 * parameter or LIFF's `liff.state` (set when a liff.line.me link carries a
 * path or query string).
 */
export function redirectTargetFromQuery(query) {
  const direct = toSafeInternalPath(firstValue(query?.redirect));
  if (direct) return direct;

  const liffState = firstValue(query?.['liff.state']);
  if (!liffState) return null;
  if (liffState.startsWith('?')) {
    return toSafeInternalPath(new URLSearchParams(liffState).get('redirect'));
  }
  return toSafeInternalPath(liffState);
}

export function rememberRedirectTarget(path) {
  const safe = toSafeInternalPath(path);
  if (!safe) return;
  try { window.sessionStorage.setItem(POST_LOGIN_REDIRECT_KEY, safe); } catch {}
}

/** URL of the LIFF entry page that returns to the current page after login. */
export function entryUrlForCurrentPage() {
  const current = toSafeInternalPath(window.location.pathname + window.location.search);
  if (!current || current === '/' || current.startsWith('/dashboard')) return ENTRY_PATH;
  return `${ENTRY_PATH}?redirect=${encodeURIComponent(current)}`;
}

function firstValue(value) {
  return Array.isArray(value) ? value[0] : value;
}
