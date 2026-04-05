<template>
  <div class="space-y-6">
    <div v-if="isLoading" class="text-center text-gray-500 py-10 animate-pulse">
      กำลังตรวจสอบข้อมูล...
    </div>

    <!-- Hero Profile -->
    <section v-else-if="studentData" class="bg-gradient-to-br from-emerald-500 to-teal-600 rounded-2xl p-6 text-white shadow-lg text-center relative overflow-hidden mt-2">
      <!-- Decorative circles -->
      <div class="absolute -top-10 -right-10 w-32 h-32 bg-white opacity-10 rounded-full"></div>
      <div class="absolute -bottom-10 -left-10 w-24 h-24 bg-white opacity-10 rounded-full"></div>
      
      <div class="w-20 h-20 bg-white rounded-full mx-auto flex items-center justify-center text-3xl mb-3 shadow-md z-10 relative">
        👦🏻
      </div>
      <h1 class="text-2xl font-bold relative z-10">{{ studentData.name }}</h1>
      <p class="opacity-90 mt-1 relative z-10 border-t border-emerald-400 pt-2 border-opacity-50 inline-block px-4">
        ห้อง {{ studentData.room || 'ยังไม่ระบุ' }} &nbsp; | &nbsp; รหัส: <span class="font-mono text-emerald-100">{{ studentData.studentId || 'รอประกาศ' }}</span>
      </p>
    </section>

    <!-- Quick Info -->
    <section v-if="studentData">
      <h2 class="text-gray-500 text-sm font-bold uppercase tracking-wider mb-2 ml-1">ข้อมูลล่าสุด</h2>
      <div class="bg-white rounded-xl shadow-sm border border-gray-100 divide-y divide-gray-100">
        <div class="p-4 flex items-center justify-between">
          <div class="flex items-center space-x-3">
            <div class="p-2 bg-emerald-50 text-emerald-600 rounded-lg">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                <path d="M2 3a1 1 0 011-1h2.153a1 1 0 01.986.836l.74 4.435a1 1 0 01-.54 1.06l-1.548.773a11.037 11.037 0 006.105 6.105l.774-1.548a1 1 0 011.059-.54l4.435.74a1 1 0 01.836.986V17a1 1 0 01-1 1h-2C7.82 18 2 12.18 2 5V3z" />
              </svg>
            </div>
            <div>
              <p class="text-xs text-gray-400">เบอร์โทรศัพท์นักเรียน</p>
              <p class="font-medium text-gray-800">{{ studentData.phone || 'ไม่ได้ระบุ' }}</p>
            </div>
          </div>
        </div>
        <div class="p-4 flex items-center justify-between">
          <div class="flex items-center space-x-3">
            <div class="p-2 bg-blue-50 text-blue-600 rounded-lg">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd" />
              </svg>
            </div>
            <div>
              <p class="text-xs text-gray-400">ผู้ปกครอง</p>
              <p class="font-medium text-gray-800">{{ guardianData.name }} ({{ formatRelation(guardianData.relationType) }})</p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Action -->
    <div class="pt-2">
      <router-link to="/register" class="w-full block text-center bg-white border border-emerald-500 text-emerald-600 font-bold py-3 px-4 rounded-xl shadow-sm hover:bg-emerald-50 focus:ring-4 focus:ring-emerald-100 transition active:scale-95">
        ✏️ แก้ไขข้อมูลเพื่อปรับปรุง
      </router-link>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useLiff } from '../composables/useLiff';

const router = useRouter();
const { initLiff, profile } = useLiff();

const isLoading = ref(true);
const studentData = ref(null);
const guardianData = ref(null);

const formatRelation = (rel) => {
  if (rel === 'Father') return 'บิดา';
  if (rel === 'Mother') return 'มารดา';
  return 'อื่นๆ';
};

onMounted(async () => {
  await initLiff();
  
  try {
    const token = profile.value?.userId === 'mock-line-uid-1234' ? 'mock-token' : 'mock-token'; // Fallback to mock token for dev demo
    
    const response = await fetch('/api/me', {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (response.ok) {
      const data = await response.json();
      studentData.value = data.student;
      guardianData.value = data.guardian;
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
