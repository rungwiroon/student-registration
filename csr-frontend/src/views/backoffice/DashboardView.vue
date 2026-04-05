<template>
  <div class="space-y-6">
    <h1 class="text-2xl font-bold text-gray-800">แดชบอร์ด</h1>
    
    <div v-if="loading" class="text-gray-500 animate-pulse">กำลังโหลดข้อมูล...</div>
    <div v-else-if="error" class="text-red-500 bg-red-50 p-4 rounded-lg">{{ error }}</div>
    <div v-else class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4 lg:gap-6">
      <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-5 lg:p-6 flex items-center justify-between">
        <div>
          <p class="text-sm text-gray-500 font-medium">นักเรียนทั้งหมด</p>
          <p class="text-3xl font-bold text-slate-800 mt-1">{{ summary?.total || 0 }}</p>
        </div>
        <div class="p-3 bg-blue-50 text-blue-600 rounded-lg text-2xl flex-shrink-0 ml-4">👥</div>
      </div>

      <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-5 lg:p-6 flex items-center justify-between">
        <div>
          <p class="text-sm text-gray-500 font-medium">สมบูรณ์แล้ว</p>
          <p class="text-3xl font-bold text-emerald-600 mt-1">{{ summary?.completed || 0 }}</p>
        </div>
        <div class="p-3 bg-emerald-50 text-emerald-600 rounded-lg text-2xl flex-shrink-0 ml-4">✅</div>
      </div>

      <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-5 lg:p-6 flex items-center justify-between sm:col-span-2 xl:col-span-1">
        <div>
          <p class="text-sm text-gray-500 font-medium">รอดำเนินการ</p>
          <p class="text-3xl font-bold text-amber-500 mt-1">{{ summary?.pending || 0 }}</p>
        </div>
        <div class="p-3 bg-amber-50 text-amber-500 rounded-lg text-2xl flex-shrink-0 ml-4">⏳</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { fetchDashboard } from '../../services/backofficeApi';
import { useLiff } from '../../composables/useLiff';
import { useRouter } from 'vue-router';

const { initLiff, getAccessToken } = useLiff();
const router = useRouter();

const loading = ref(true);
const error = ref('');
const summary = ref(null);

onMounted(async () => {
  await initLiff();
  const token = getAccessToken();
  if (!token) {
    router.push('/');
    return;
  }
  
  try {
    summary.value = await fetchDashboard(token);
  } catch (err) {
    if (err.status === 403) {
      error.value = 'ไม่มีสิทธิ์เข้าถึงส่วนนี้';
    } else {
      error.value = 'เกิดข้อผิดพลาดในการโหลดข้อมูล';
    }
  } finally {
    loading.value = false;
  }
});
</script>
