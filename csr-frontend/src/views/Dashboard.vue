<template>
  <div class="space-y-6">
    <div v-if="isLoading" class="text-center text-gray-500 py-10 animate-pulse">
      <p>กำลังตรวจสอบข้อมูล...</p>
      <p v-if="isAndroid" class="text-xs mt-2 text-gray-400">เชื่อมต่อ LINE บน Android อาจใช้เวลาสักครู่</p>
      <button v-if="showReloadButton" @click="reloadPage" class="mt-4 rounded-lg bg-brand-primary text-white px-4 py-2 text-sm font-bold active:scale-95 transition">
        โหลดใหม่
      </button>
    </div>

    <!-- LIFF Error -->
    <div v-else-if="liffError" class="rounded-xl border border-red-200 bg-red-50 p-4 text-red-700 text-sm">
      <p class="font-bold mb-1">LIFF Error</p>
      <p>{{ liffError.message }}</p>
      <p class="mt-2 text-xs text-red-500">ua: {{ userAgentSnippet }}</p>
      <button @click="retry" class="mt-3 w-full rounded-lg bg-red-600 text-white py-2 text-sm font-bold active:scale-95 transition">
        ลองใหม่
      </button>
      <button v-if="isAndroid" @click="login" class="mt-2 w-full rounded-lg bg-green-600 text-white py-2 text-sm font-bold active:scale-95 transition">
        เข้าสู่ระบบ LINE
      </button>
    </div>

    <!-- Hero Profile -->
    <section v-else-if="studentData" class="rounded-2xl bg-gradient-to-br from-brand-primary to-brand-primary-strong p-6 text-white shadow-lg text-center relative overflow-hidden mt-2">
      <!-- Decorative circles -->
      <div class="absolute -top-10 -right-10 h-32 w-32 rounded-full bg-brand-secondary opacity-15"></div>
      <div class="absolute -bottom-10 -left-10 h-24 w-24 rounded-full bg-white opacity-10"></div>

      <div class="w-20 h-20 bg-white rounded-full mx-auto flex items-center justify-center mb-3 shadow-md z-10 relative overflow-hidden">
        <img v-if="photoUrl" :src="photoUrl" alt="รูปนักเรียน" class="w-full h-full object-cover" />
        <span v-else class="text-3xl">👦🏻</span>
      </div>
      <h1 class="text-2xl font-bold relative z-10">{{ studentName }}</h1>
      <p class="mt-1 inline-block border-t border-white/30 px-4 pt-2 opacity-90 relative z-10">
        ห้อง {{ studentData.room || 'ยังไม่ระบุ' }} &nbsp; | &nbsp; รหัส: <span class="font-mono text-brand-secondary-soft">{{ studentData.studentId || 'รอประกาศ' }}</span>
      </p>
    </section>

    <!-- Quick Info -->
    <section v-if="studentData">
      <h2 class="mb-2 ml-1 text-sm font-bold uppercase tracking-wider text-text-secondary">ข้อมูลล่าสุด</h2>
      <div class="divide-y divide-border rounded-xl border border-border bg-surface shadow-sm">
        <div class="p-4 flex items-center justify-between">
          <div class="flex items-center space-x-3">
            <div class="rounded-lg bg-brand-primary-soft p-2 text-brand-primary-strong">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                <path d="M2 3a1 1 0 011-1h2.153a1 1 0 01.986.836l.74 4.435a1 1 0 01-.54 1.06l-1.548.773a11.037 11.037 0 006.105 6.105l.774-1.548a1 1 0 011.059-.54l4.435.74a1 1 0 01.836.986V17a1 1 0 01-1 1h-2C7.82 18 2 12.18 2 5V3z" />
              </svg>
            </div>
            <div>
              <p class="text-xs text-text-secondary">เบอร์โทรศัพท์นักเรียน</p>
              <p class="font-medium text-text-primary">{{ studentData.phone || 'ไม่ได้ระบุ' }}</p>
            </div>
          </div>
        </div>
        <div class="p-4 flex items-center justify-between">
          <div class="flex items-center space-x-3">
            <div class="rounded-lg bg-brand-secondary-soft p-2 text-brand-secondary-strong">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd" />
              </svg>
            </div>
            <div>
              <p class="text-xs font-medium text-brand-secondary-strong">ผู้ปกครอง</p>
              <p class="font-medium text-text-primary">{{ guardianName }} ({{ formatRelation(guardianData?.relationType) }})</p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Not registered -->
    <div v-else-if="!isLoading && !liffError" class="text-center py-8">
      <div class="mb-3 text-6xl">📝</div>
      <p class="text-lg font-bold text-gray-700 mb-1">ยังไม่ได้ลงทะเบียน</p>
      <p class="text-sm text-gray-500 mb-4">ลงทะเบียนเพื่อใช้งานระบบ</p>
    </div>

    <!-- Action Buttons -->
    <div class="space-y-3 pt-2">
      <!-- Shirt Order — always visible, prominent -->
      <router-link to="/shirt-order" class="block w-full rounded-2xl bg-gradient-to-r from-brand-secondary to-brand-secondary-strong px-6 py-5 text-center font-bold text-white shadow-lg transition hover:brightness-110 focus:ring-4 focus:ring-focus-ring active:scale-95">
        <div class="text-lg">สั่งเสื้อรุ่น SKN50 ม. 1/2</div>
        <div class="text-xs font-normal opacity-90 mt-1">คลิกที่นี่เพื่อสั่งซื้อ</div>
      </router-link>

      <!-- Register / Edit Profile -->
      <router-link v-if="!studentData" to="/register" class="block w-full rounded-xl bg-brand-primary px-4 py-3 text-center font-bold text-white shadow-sm transition hover:bg-brand-primary-strong focus:ring-4 focus:ring-focus-ring active:scale-95">
        📝 ลงทะเบียนนักเรียน
      </router-link>
      <router-link v-else to="/profile/edit" class="block w-full rounded-xl border border-action-primary bg-surface px-4 py-3 text-center font-bold text-action-primary shadow-sm transition hover:bg-brand-primary-soft focus:ring-4 focus:ring-focus-ring active:scale-95">
        ✏️ แก้ไขข้อมูล
      </router-link>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue';
import { useRouter } from 'vue-router';
import { useLiff } from '../composables/useLiff';
import { fetchProtectedPhotoUrl } from '../services/registrationApi';

const router = useRouter();
const { getAccessToken, error: liffError, isReady, login } = useLiff();

const isLoading = ref(true);
const studentData = ref(null);
const guardianData = ref(null);
const photoUrl = ref(null);
const userAgentSnippet = ref(navigator.userAgent.slice(0, 80));
const isAndroid = computed(() => /android/i.test(navigator.userAgent));
const showReloadButton = ref(false);
let reloadTimer = null;

const studentName = computed(() => {
  if (!studentData.value) return '-';
  const parts = [studentData.value.firstName, studentData.value.lastName].filter(Boolean);
  return parts.length > 0 ? parts.join(' ') : '-';
});

const guardianName = computed(() => {
  if (!guardianData.value) return '-';
  const parts = [guardianData.value.firstName, guardianData.value.lastName].filter(Boolean);
  return parts.length > 0 ? parts.join(' ') : '-';
});

const formatRelation = (rel) => {
  if (rel === 'Father') return 'บิดา';
  if (rel === 'Mother') return 'มารดา';
  return 'อื่นๆ';
};

function hasCallbackParams() {
  const url = new URL(window.location.href);
  const code = url.searchParams.get('code');
  const state = url.searchParams.get('state');
  const liffClientId = url.searchParams.get('liffClientId');
  return !!(code && state && liffClientId);
}

function redirectToEntry(preserveQuery = false) {
  let target = '/liff-entry.html';
  if (preserveQuery) {
    target += window.location.search;
  }
  console.log('[Dashboard] redirect to', target);
  window.location.href = target;
}

async function loadData() {
  isLoading.value = true;
  try {
    const token = getAccessToken();
    console.log('[Dashboard] token=', token ? 'present' : 'null');
    if (!token) {
      console.log('[Dashboard] no token, redirect to entry page');
      redirectToEntry();
      return;
    }

    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 15000);

    let response;
    try {
      response = await fetch('/api/me', {
        headers: { 'Authorization': `Bearer ${token}` },
        signal: controller.signal
      });
    } finally {
      clearTimeout(timeout);
    }

    console.log('[Dashboard] /api/me status=', response.status);

    if (response.ok) {
      const data = await response.json();
      console.log('[Dashboard] /api/me data=', JSON.stringify({ student: !!data.student, guardians: data.guardians?.length }));
      studentData.value = data.student;
      guardianData.value = data.guardians?.[0] || null;

      if (data.student?.photoUrl) {
        try {
          photoUrl.value = await fetchProtectedPhotoUrl(token, data.student.photoUrl);
        } catch (err) {
          console.warn('Failed to load student photo', err);
        }
      }
    } else if (response.status === 401 || response.status === 404) {
      console.log('[Dashboard] 401/404, user not registered yet — staying on dashboard');
      // Don't redirect to /register — user can still browse and order shirts
    } else {
      console.error('[Dashboard] /api/me failed', response.status, await response.text().catch(() => ''));
    }
  } catch (error) {
    console.error('[Dashboard] API Error:', error);
  } finally {
    isLoading.value = false;
    if (reloadTimer) clearTimeout(reloadTimer);
    console.log('[Dashboard] load complete, studentData=', !!studentData.value);
  }
}

function startReloadTimer() {
  showReloadButton.value = false;
  if (reloadTimer) clearTimeout(reloadTimer);
  reloadTimer = setTimeout(() => { showReloadButton.value = true; }, 10000);
}

function reloadPage() {
  console.log('[Dashboard] reloadPage clicked');
  window.location.reload();
}

async function handleInit() {
  isLoading.value = true;
  startReloadTimer();
  console.log('[Dashboard] init start');

  // If callback params present, delegate to entry page
  if (hasCallbackParams()) {
    console.log('[Dashboard] callback params detected, delegating to entry page');
    redirectToEntry(true);
    return;
  }

  // Read token from localStorage via composable
  const token = getAccessToken();
  console.log('[Dashboard] token from storage=', token ? 'present' : 'null');

  if (!token) {
    console.log('[Dashboard] no token, redirect to entry page');
    redirectToEntry();
    return;
  }

  await loadData();
}

async function retry() {
  console.log('[Dashboard] retry clicked');
  redirectToEntry();
}

onMounted(() => {
  handleInit();
});

onBeforeUnmount(() => {
  if (reloadTimer) clearTimeout(reloadTimer);
  if (photoUrl.value) {
    URL.revokeObjectURL(photoUrl.value);
  }
});
</script>
