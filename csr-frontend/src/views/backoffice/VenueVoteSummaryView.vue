<template>
  <div class="space-y-6">
    <h1 class="text-2xl font-bold text-gray-800">ผลโหวตสถานที่จัดกิจกรรม</h1>

    <div v-if="loading" class="text-gray-500 animate-pulse">กำลังโหลดข้อมูล...</div>
    <div v-else-if="error" class="text-red-500 bg-red-50 p-4 rounded-lg">{{ error }}</div>
    <template v-else-if="summary">
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 lg:gap-6">
        <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-5 lg:p-6">
          <p class="text-sm text-gray-500 font-medium">ครอบครัวที่โหวตแล้ว</p>
          <p class="text-3xl font-bold text-slate-800 mt-1">
            {{ summary.totalVotes }} <span class="text-base font-medium text-gray-500">/ {{ summary.totalFamilies }}</span>
          </p>
        </div>
        <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-5 lg:p-6">
          <p class="text-sm text-gray-500 font-medium">คนที่จะไปทั้งหมด</p>
          <p class="text-3xl font-bold text-slate-800 mt-1">
            {{ summary.totalPeople }} <span class="text-base font-medium text-gray-500">คน</span>
          </p>
        </div>
        <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-5 lg:p-6">
          <p class="text-sm text-gray-500 font-medium">สถานะ</p>
          <p class="text-xl font-bold mt-1" :class="summary.isClosed ? 'text-red-600' : 'text-emerald-600'">
            {{ summary.isClosed ? 'ปิดโหวตแล้ว' : 'เปิดโหวตอยู่' }}
          </p>
          <p class="text-sm text-gray-500 mt-1">{{ closesAtText ? `ปิดโหวต ${closesAtText}` : 'ไม่ได้กำหนดวันปิด' }}</p>
        </div>
      </div>

      <div class="bg-white rounded-xl shadow-sm border border-gray-100 divide-y divide-gray-100">
        <div v-for="row in rows" :key="row.id" class="p-5">
          <div class="flex items-baseline justify-between gap-4">
            <div class="min-w-0">
              <p class="font-bold text-slate-800">{{ row.name }}</p>
              <p class="text-sm text-gray-500">{{ row.area }}</p>
            </div>
            <p class="shrink-0 text-lg font-bold text-slate-800">
              {{ row.count }} <span class="text-sm font-medium text-gray-500">โหวต · {{ row.people }} คน ({{ row.percent }}%)</span>
            </p>
          </div>
          <div class="mt-2 h-2 overflow-hidden rounded-full bg-gray-200">
            <div class="h-full rounded-full bg-emerald-500" :style="{ width: `${row.percent}%` }"></div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue';
import { fetchVenueVoteSummary } from '../../services/backofficeApi';
import { UnauthorizedError } from '../../services/apiClient';
import { useLiff } from '../../composables/useLiff';

const { getAccessToken, login } = useLiff();

const loading = ref(true);
const error = ref('');
const summary = ref(null);

const rows = computed(() => {
  const total = summary.value?.totalVotes ?? 0;
  return (summary.value?.venues ?? [])
    .map((venue) => {
      const entry = summary.value.counts.find((c) => c.venueId === venue.id);
      const count = entry?.count ?? 0;
      const people = entry?.people ?? 0;
      return { ...venue, count, people, percent: total > 0 ? Math.round((count / total) * 100) : 0 };
    })
    .sort((a, b) => b.count - a.count);
});

const closesAtText = computed(() => {
  if (!summary.value?.closesAtUtc) return '';
  return new Date(summary.value.closesAtUtc).toLocaleString('th-TH', {
    timeZone: 'Asia/Bangkok',
    dateStyle: 'long',
    timeStyle: 'short'
  });
});

onMounted(async () => {
  const token = getAccessToken();
  if (!token) { login(); return; }

  try {
    summary.value = await fetchVenueVoteSummary(token);
  } catch (err) {
    if (err instanceof UnauthorizedError) return;
    error.value = err.status === 403 ? 'ไม่มีสิทธิ์เข้าถึงส่วนนี้' : 'เกิดข้อผิดพลาดในการโหลดข้อมูล';
  } finally {
    loading.value = false;
  }
});
</script>
