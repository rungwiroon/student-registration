import { ref } from 'vue';

export const LIFF_COMPOSABLE_VERSION = '2026-05-02-v3';

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

export function useLiff() {
  if (useMockLiff) {
    profile.value = { userId: 'mock-line-uid-1234', displayName: 'Mock Parent' };
    accessToken.value = 'mock-token';
    isInClient.value = false;
    isReady.value = true;
  }

  const getAccessToken = () => {
    if (useMockLiff) return 'mock-token';
    const token = typeof window !== 'undefined' ? window.localStorage.getItem(LIFF_TOKEN_KEY) : '';
    return token || accessToken.value;
  };

  const closeWindow = () => window.liff?.closeWindow?.();
  const login = () => {
    // Delegate to entry page for fresh LIFF init
    if (typeof window !== 'undefined') {
      window.location.href = '/liff-entry.html';
    }
  };

  return { isReady, profile, error, getAccessToken, isInClient, closeWindow, login };
}
