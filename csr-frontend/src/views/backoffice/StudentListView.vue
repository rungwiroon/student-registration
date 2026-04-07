<template>
  <div class="space-y-6">
    <div class="flex justify-between items-center">
      <h1 class="text-2xl font-bold text-gray-800">รายชื่อนักเรียน</h1>
      <button
        v-if="showExportButton"
        @click="handleExport"
        :disabled="exporting || filteredStudents.length === 0"
        class="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-lg border transition"
        :class="exporting || filteredStudents.length === 0
          ? 'bg-gray-100 text-gray-400 border-gray-200 cursor-not-allowed'
          : 'bg-white text-slate-700 border-slate-300 hover:bg-slate-50 active:bg-slate-100'"
      >
        <svg v-if="!exporting" xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3M3 17v3a2 2 0 002 2h14a2 2 0 002-2v-3" />
        </svg>
        <svg v-else class="animate-spin h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
        </svg>
        {{ exporting ? 'กำลังส่งออก...' : 'Export Excel' }}
      </button>
    </div>

    <div v-if="loading" class="text-gray-500 animate-pulse">กำลังโหลดข้อมูล...</div>
    <div v-else-if="error" class="text-red-500 bg-red-50 p-4 rounded-lg">{{ error }}</div>

    <div v-else class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
      <!-- Search/Filter -->
      <div class="p-4 border-b border-gray-200 bg-gray-50">
        <input type="text" v-model="search" placeholder="ค้นหา ชื่อ, เลขประจำตัว..." class="w-full md:w-1/3 px-4 py-2 border rounded-lg focus:ring-2 focus:ring-slate-500 outline-none">
      </div>

      <!-- Mobile Card List -->
      <div class="divide-y divide-gray-100 lg:hidden bg-white">
        <router-link
          v-for="student in filteredStudents"
          :key="'mob-'+student.id"
          :to="`/backoffice/students/${student.id}`"
          class="block p-4 hover:bg-slate-50 transition active:bg-slate-100"
        >
          <div class="flex justify-between items-start mb-2">
            <div>
              <p class="font-bold text-slate-800 text-base leading-tight">{{ student.name }}</p>
              <p class="text-xs text-gray-500 mt-1">รหัส: <span class="font-mono text-slate-600">{{ student.studentId }}</span> · เลขที่ <span class="text-slate-600">{{ student.newNo ?? '-' }}</span></p>
            </div>
            <span class="px-2.5 py-1 text-[10px] font-bold rounded-full uppercase tracking-wide whitespace-nowrap"
                  :class="student.status === 'Pending' ? 'bg-amber-100 text-amber-700' : 'bg-emerald-100 text-emerald-700'">
              {{ student.status }}
            </span>
          </div>
          <div class="flex items-center text-xs text-gray-400">
            <span v-if="student.nickname">ชื่อเล่น: <span class="text-gray-600">{{ student.nickname }}</span></span>
            <span v-else>ไม่มีชื่อเล่น</span>
            <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 ml-auto text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </div>
        </router-link>
      </div>

      <!-- Desktop Table -->
      <div class="overflow-x-auto hidden lg:block">
        <table class="w-full text-left text-sm text-gray-600">
          <thead class="bg-gray-50 text-gray-700 font-medium">
            <tr>
              <th class="px-6 py-4 border-b">รหัสนักเรียน</th>
              <th class="px-6 py-4 border-b">ชื่อ - นามสกุล</th>
              <th class="px-6 py-4 border-b">ชื่อเล่น</th>
              <th class="px-6 py-4 border-b">เลขที่</th>
              <th class="px-6 py-4 border-b">สถานะ</th>
              <th class="px-6 py-4 border-b text-center">จัดการ</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-for="student in filteredStudents" :key="student.id" class="hover:bg-slate-50 transition">
              <td class="px-6 py-4 font-mono text-slate-600">{{ student.studentId }}</td>
              <td class="px-6 py-4 font-bold text-slate-800">{{ student.name }}</td>
              <td class="px-6 py-4">{{ student.nickname || '-' }}</td>
              <td class="px-6 py-4 text-center">{{ student.newNo ?? '-' }}</td>
              <td class="px-6 py-4">{{ student.nickname || '-' }}</td>
              <td class="px-6 py-4">
                <span class="px-2.5 py-1 text-xs font-bold rounded-full uppercase tracking-wide"
                      :class="student.status === 'Pending' ? 'bg-amber-100 text-amber-700' : 'bg-emerald-100 text-emerald-700'">
                  {{ student.status }}
                </span>
              </td>
              <td class="px-6 py-4 text-center">
                <router-link :to="`/backoffice/students/${student.id}`" class="inline-flex items-center text-blue-600 hover:text-blue-800 font-medium px-3 py-1.5 rounded-lg hover:bg-blue-50 transition">
                  เปิดดูรายละเอียด
                </router-link>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="filteredStudents.length === 0" class="p-10 text-center text-gray-400 bg-white">
        <div class="text-4xl mb-3">🔍</div>
        <p>ไม่พบข้อมูลนักเรียนที่ค้นหา</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue';
import { fetchStudents, exportStudentsExcel } from '../../services/backofficeApi';
import { useLiff } from '../../composables/useLiff';
import { useBackofficeAuth } from '../../composables/useBackofficeAuth';
import { useRouter } from 'vue-router';

const { initLiff, getAccessToken } = useLiff();
const { loadCurrentUser, canViewFullProfile, canExportStudentList } = useBackofficeAuth();
const router = useRouter();

const loading = ref(true);
const error = ref('');
const students = ref([]);
const search = ref('');
const exporting = ref(false);

const showExportButton = computed(() => canExportStudentList());

const filteredStudents = computed(() => {
  if (!search.value) return students.value;
  const lowerSearch = search.value.toLowerCase();
  return students.value.filter(s =>
    (s.studentId && s.studentId.includes(lowerSearch)) ||
    (s.name && s.name.toLowerCase().includes(lowerSearch)) ||
    (s.nickname && s.nickname.toLowerCase().includes(lowerSearch))
  );
});

async function handleExport() {
  if (exporting.value) return;
  exporting.value = true;
  try {
    const token = getAccessToken();
    await exportStudentsExcel(token, search.value);
  } catch (err) {
    if (err.status === 403) {
      alert('ไม่มีสิทธิ์ส่งออกข้อมูล');
    } else {
      alert('เกิดข้อผิดพลาดในการส่งออก');
    }
  } finally {
    exporting.value = false;
  }
}

onMounted(async () => {
  await initLiff();
  const token = getAccessToken();
  if (!token) {
    router.push('/');
    return;
  }

  try {
    await loadCurrentUser(token);
    students.value = await fetchStudents(token);
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
