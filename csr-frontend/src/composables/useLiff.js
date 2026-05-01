import { ref } from 'vue';
import liff from '@line/liff';

const isReady = ref(false);
const profile = ref(null);
const accessToken = ref('');
const error = ref(null);
const isInClient = ref(false);
const useMockLiff = import.meta.env.VITE_USE_MOCK_LIFF === 'true';

export function useLiff() {
  const initLiff = async () => {
    if (isReady.value) return;

    try {
      const liffId = import.meta.env.VITE_LIFF_ID;

      if (useMockLiff) {
        profile.value = { userId: 'mock-line-uid-1234', displayName: 'Mock Parent' };
        accessToken.value = 'mock-token';
        isInClient.value = false;
        isReady.value = true;
        return;
      }

      if (!liffId) {
        throw new Error('VITE_LIFF_ID is required when VITE_USE_MOCK_LIFF is disabled.');
      }

      await liff.init({ liffId });
      isInClient.value = liff.isInClient();
      if (!liff.isLoggedIn()) {
        liff.login();
      } else {
        profile.value = await liff.getProfile();
        accessToken.value = liff.getAccessToken() ?? '';
        isReady.value = true;
      }
    } catch (err) {
      error.value = err;
      console.error('LIFF initialization failed', err);
      isReady.value = true;
    }
  };

  const getAccessToken = () => accessToken.value;

  const closeWindow = () => {
    if (useMockLiff) {
      console.log('[Mock LIFF] closeWindow() called');
      return;
    }
    liff.closeWindow();
  };

  return { initLiff, isReady, profile, error, getAccessToken, isInClient, closeWindow };
}
