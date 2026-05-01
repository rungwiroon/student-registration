<template>
  <div class="space-y-6">
    <h1 class="text-2xl font-bold text-gray-800">ผู้ใช้ที่ลงทะเบียน</h1>

    <div v-if="loading" class="text-gray-500 animate-pulse">กำลังโหลดข้อมูล...</div>
    <div v-else-if="error" class="text-red-500 bg-red-50 p-4 rounded-lg">{{ error }}</div>

    <div v-else class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
      <!-- Mobile Card List -->
      <div class="divide-y divide-gray-100 lg:hidden">
        <div v-for="user in users" :key="user.lineUserId" class="p-4 space-y-2">
          <div class="flex justify-between items-start">
            <div>
              <p class="font-bold text-slate-800">{{ user.name || '-' }}</p>
              <p class="text-xs text-gray-500">{{ formatRelation(user.relationType) }}</p>
            </div>
            <span class="text-xs font-mono text-gray-400">#{{ user.guardianOrder }}</span>
          </div>
          <div class="flex items-center space-x-2">
            <code class="text-xs font-mono bg-gray-100 px-2 py-1 rounded">{{ user.lineUserId }}</code>
            <button @click="copy(user.lineUserId)" class="text-xs" :class="copiedId === user.lineUserId ? 'text-emerald-600 font-medium' : 'text-blue-600 underline'">{{ copiedId === user.lineUserId ? 'คัดลอกแล้ว' : 'คัดลอก' }}</button>
          </div>
          <p class="text-xs text-gray-500">{{ user.phone || '-' }}</p>
        </div>
        <div v-if="users.length === 0" class="p-10 text-center text-gray-400">
          <p>ยังไม่มีผู้ใช้ที่ลงทะเบียน</p>
        </div>
      </div>

      <!-- Desktop Table -->
      <div class="overflow-x-auto hidden lg:block">
        <table class="w-full text-left text-sm text-gray-600">
          <thead class="bg-gray-50 text-gray-700 font-medium">
            <tr>
              <th class="px-6 py-4 border-b">LINE User ID</th>
              <th class="px-6 py-4 border-b">ชื่อ</th>
              <th class="px-6 py-4 border-b">ความสัมพันธ์</th>
              <th class="px-6 py-4 border-b">เบอร์โทร</th>
              <th class="px-6 py-4 border-b">ลำดับ</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-for="user in users" :key="user.lineUserId" class="hover:bg-slate-50 transition">
              <td class="px-6 py-4 font-mono text-xs">
                <div class="flex items-center space-x-2">
                  <code class="bg-gray-100 px-2 py-1 rounded">{{ user.lineUserId }}</code>
                  <button @click="copy(user.lineUserId)" class="text-xs" :class="copiedId === user.lineUserId ? 'text-emerald-600 font-medium' : 'text-blue-600 hover:text-blue-800 underline'">{{ copiedId === user.lineUserId ? 'คัดลอกแล้ว' : 'คัดลอก' }}</button>
                </div>
              </td>
              <td class="px-6 py-4 font-bold text-slate-800">{{ user.name || '-' }}</td>
              <td class="px-6 py-4">{{ formatRelation(user.relationType) }}</td>
              <td class="px-6 py-4">{{ user.phone || '-' }}</td>
              <td class="px-6 py-4">{{ user.guardianOrder }}</td>
            </tr>
            <tr v-if="users.length === 0">
              <td colspan="5" class="px-6 py-10 text-center text-gray-400">ยังไม่มีผู้ใช้ที่ลงทะเบียน</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { fetchRegisteredUsers } from '../../services/backofficeApi';
import { useLiff } from '../../composables/useLiff';
import { useBackofficeAuth } from '../../composables/useBackofficeAuth';
import { useRouter } from 'vue-router';

const { initLiff, getAccessToken } = useLiff();
const { loadCurrentUser, canManageStaff } = useBackofficeAuth();
const router = useRouter();

const loading = ref(true);
const error = ref('');
const users = ref([]);
const copiedId = ref(null);

const formatRelation = (rel) => {
  if (rel === 'Father') return 'บิดา';
  if (rel === 'Mother') return 'มารดา';
  return rel || '-';
};

const copy = async (text) => {
  try {
    await navigator.clipboard.writeText(text);
    copiedId.value = text;
    setTimeout(() => { if (copiedId.value === text) copiedId.value = null; }, 1200);
  } catch {
    // ignore
  }
};

onMounted(async () => {
  await initLiff();
  const token = getAccessToken();
  if (!token) {
    router.push('/');
    return;
  }

  try {
    await loadCurrentUser(token);
    if (!canManageStaff()) {
      error.value = 'ไม่มีสิทธิ์เข้าถึงส่วนนี้';
      loading.value = false;
      return;
    }
    users.value = await fetchRegisteredUsers(token);
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
