import { ref } from 'vue';

export const LIFF_COMPOSABLE_VERSION = '2026-05-02-v3';

const LIFF_ID = import.meta.env.VITE_LIFF_ID || '2009916202-YGxGSKdi';
const LIFF_TOKEN_KEY = 'liff_access_token';
const LIFF_USER_ID_KEY = 'liff_user_id';
const LIFF_DISPLAY_NAME_KEY = 'liff_display_name';
const LIFF_IN_CLIENT_KEY = 'liff_is_in_client';

function loadFromStorage() {
  if (typeof window === 'undefined') {
    return { token: '', userId: '', displayName: '', inClient: false };
  }
  return {
    token: window.localStorage.getItem(LIFF_TOKEN_KEY) || '',
    userId: window.localStorage.getItem(LIFF_USER_ID_KEY) || '',
    displayName: window.localStorage.getItem(LIFF_DISPLAY_NAME_KEY) || '',
    inClient: window.localStorage.getItem(LIFF_IN_CLIENT_KEY) === 'true'
  };
}

const stored = loadFromStorage();

const isReady = ref(!!stored.token);
const profile = ref(stored.userId ? { userId: stored.userId, displayName: stored.displayName } : null);
const accessToken = ref(stored.token);
const error = ref(null);
const isInClient = ref(stored.inClient);
const useMockLiff = import.meta.env.VITE_USE_MOCK_LIFF === 'true';

let initPromise = null;

export function useLiff() {
  if (useMockLiff) {
    profile.value = { userId: 'mock-line-uid-1234', displayName: 'Mock Parent' };
    accessToken.value = 'mock-token';
    isInClient.value = false;
    isReady.value = true;
  }

  const getAccessToken = () => {
    if (useMockLiff) return 'mock-token';
    return accessToken.value;
  };

  const closeWindow = () => window.liff?.closeWindow?.();
  const login = () => {
    if (typeof window !== 'undefined') {
      window.location.href = '/liff-entry.html';
    }
  };

  const init = async () => {
    if (useMockLiff) return;
    if (initPromise) return initPromise;

    initPromise = (async () => {
      if (typeof window === 'undefined' || !window.liff || typeof window.liff.init !== 'function') {
        return;
      }

      try {
        await window.liff.init({ liffId: LIFF_ID });
      } catch (e) {
        error.value = e;
        return;
      }

      const loggedIn = window.liff.isLoggedIn?.() ?? false;
      if (!loggedIn) {
        // External browser without active LIFF session — keep existing localStorage
        // token so the app can still function. init() is best-effort refresh only.
        return;
      }

      // Get fresh token
      let token = null;
      try {
        token = window.liff.getAccessToken();
      } catch (e) {
        console.warn('[useLiff] getAccessToken error:', e);
      }

      // Get fresh profile
      let freshProfile = null;
      try {
        freshProfile = await window.liff.getProfile();
      } catch (e) {
        console.warn('[useLiff] getProfile error:', e);
      }

      const inClient = window.liff.isInClient?.() ?? false;

      // Update reactive refs
      accessToken.value = token || '';
      profile.value = freshProfile?.userId
        ? { userId: freshProfile.userId, displayName: freshProfile.displayName || '' }
        : null;
      isInClient.value = inClient;
      isReady.value = !!token;

      // Persist to localStorage
      try {
        if (token) window.localStorage.setItem(LIFF_TOKEN_KEY, token);
        if (freshProfile?.userId) window.localStorage.setItem(LIFF_USER_ID_KEY, freshProfile.userId);
        if (freshProfile?.displayName) window.localStorage.setItem(LIFF_DISPLAY_NAME_KEY, freshProfile.displayName);
        window.localStorage.setItem(LIFF_IN_CLIENT_KEY, String(inClient));
      } catch {}

    })();

    return initPromise;
  };

  return { isReady, profile, error, getAccessToken, isInClient, closeWindow, login, init };
}
