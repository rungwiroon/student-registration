import { ref } from 'vue';
import liff from '@line/liff';

export function useLiff() {
  const isReady = ref(false);
  const profile = ref(null);
  const error = ref(null);

  const initLiff = async () => {
    try {
      const liffId = import.meta.env.VITE_LIFF_ID;
      
      // ถ้าไม่มี LIFF ID ในระบบ (รัน Local) ให้เข้าโหมด Mock อัตโนมัติ
      if (!liffId) {
        console.warn('LIFF ID is missing. Running in Mock Mode for local testing.');
        profile.value = { userId: 'mock-line-uid-1234', displayName: 'Mock Parent' };
        // หน่วงเวลาเล็กน้อยเพื่อให้เห็นว่ามีการเชื่อมต่อ
        setTimeout(() => { isReady.value = true; }, 500);
        return;
      }

      await liff.init({ liffId });
      if (!liff.isLoggedIn()) {
        liff.login();
      } else {
        profile.value = await liff.getProfile();
        isReady.value = true;
      }
    } catch (err) {
      error.value = err;
      console.error('LIFF initialization failed', err);
      // Fallback ถ้าพัง ให้เข้าสู Mock Mode เผื่อฉุกเฉินบน Local
      profile.value = { userId: 'fallback-mock-uid', displayName: 'Fallback Parent' };
      isReady.value = true;
    }
  };

  return { initLiff, isReady, profile, error };
}
