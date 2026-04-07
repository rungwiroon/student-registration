<template>
  <div class="space-y-6">
    <div v-if="isLoading" class="text-center text-gray-500 py-10 animate-pulse">
      กำลังตรวจสอบข้อมูล...
    </div>

    <!-- Hero Profile -->
    <section v-else-if="studentData" class="rounded-2xl bg-gradient-to-br from-brand-primary to-brand-primary-strong p-6 text-white shadow-lg text-center relative overflow-hidden mt-2">
      <!-- Decorative circles -->
      <div class="absolute -top-10 -right-10 h-32 w-32 rounded-full bg-brand-secondary opacity-15"></div>
      <div class="absolute -bottom-10 -left-10 h-24 w-24 rounded-full bg-white opacity-10"></div>
      
      <div class="w-20 h-20 bg-white rounded-full mx-auto flex items-center justify-center text-3xl mb-3 shadow-md z-10 relative">
        👦🏻
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

    <!-- Action -->
    <div class="pt-2">
      <router-link to="/profile/edit" class="block w-full rounded-xl border border-action-primary bg-surface px-4 py-3 text-center font-bold text-action-primary shadow-sm transition hover:bg-brand-primary-soft focus:ring-4 focus:ring-focus-ring active:scale-95">
        ✏️ แก้ไขข้อมูล
      </router-link>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useLiff } from '../composables/useLiff';

const router = useRouter();
const { initLiff, getAccessToken } = useLiff();

const isLoading = ref(true);
const studentData = ref(null);
const guardianData = ref(null);

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

onMounted(async () => {
  await initLiff();
  
  try {
    const token = getAccessToken();
    if (!token) {
      router.push('/register');
      return;
    }
    
    const response = await fetch('/api/me', {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (response.ok) {
      const data = await response.json();
      studentData.value = data.student;
      // Get primary guardian (first in array)
      guardianData.value = data.guardians?.[0] || null;
    } else if (response.status === 401 || response.status === 404) {
      // If not registered or unauthorized
      router.push('/register');
    } else {
      console.error('Failed to load profile');
    }
  } catch (error) {
    console.error('API Error:', error);
  } finally {
    isLoading.value = false;
  }
});
</script>
