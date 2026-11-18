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

      <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-5 space-y-4">
        <h2 class="text-lg font-bold text-slate-800">รายครอบครัว</h2>

        <div class="flex flex-wrap items-center gap-2">
          <button v-for="opt in statusOptions" :key="opt.value" type="button"
                  class="px-3 py-1.5 rounded-full text-sm border transition"
                  :class="statusFilter === opt.value ? 'bg-slate-700 text-white border-slate-700' : 'bg-white text-slate-600 border-gray-300 hover:bg-slate-50'"
                  @click="statusFilter = opt.value">{{ opt.label }}</button>
          <span class="text-gray-300">|</span>
          <button v-for="venue in summary.venues" :key="venue.id" type="button"
                  class="px-3 py-1.5 rounded-full text-sm border transition"
                  :class="venueFilter === venue.id ? 'bg-emerald-600 text-white border-emerald-600' : 'bg-white text-slate-600 border-gray-300 hover:bg-slate-50'"
                  @click="venueFilter = venueFilter === venue.id ? '' : venue.id">{{ venue.name }}</button>
        </div>

        <input v-model="search" type="search" aria-label="ค้นหาครอบครัว" placeholder="ค้นหาชื่อ / รหัสนักเรียน / ห้อง"
               class="w-full sm:max-w-sm px-3 py-2 border rounded-lg text-sm focus:ring-2 focus:ring-slate-500 outline-none" />

        <p v-if="familiesError" class="text-red-500 text-sm">{{ familiesError }}</p>
        <p v-else class="text-sm text-gray-600" data-testid="family-summary">
          แสดง {{ filteredFamilies.length }} ครอบครัว · รวม {{ filteredPeople }} คน
        </p>

        <!-- Mobile cards -->
        <div class="divide-y divide-gray-100 lg:hidden">
          <div v-for="f in filteredFamilies" :key="f.studentId" class="py-3">
            <div class="flex justify-between gap-3">
              <p class="font-bold text-slate-800">{{ f.name || '-' }}</p>
              <p class="shrink-0 text-sm text-gray-500">{{ roomText(f) }}</p>
            </div>
            <p class="text-xs text-gray-400">{{ f.studentId }}</p>
            <p v-if="f.hasVoted" class="text-sm mt-1">
              <span class="text-slate-700">{{ venueName(f.venueId) }}</span>
              · <span class="font-medium">{{ peopleText(f) }}</span>
            </p>
            <p v-else class="text-sm mt-1 text-gray-400">ยังไม่โหวต</p>
          </div>
        </div>

        <!-- Desktop table -->
        <div class="overflow-x-auto hidden lg:block">
          <table class="w-full text-left text-sm text-gray-600">
            <thead class="bg-gray-50 text-gray-700 font-medium">
              <tr>
                <th class="px-4 py-3 border-b">ห้อง / เลขที่</th>
                <th class="px-4 py-3 border-b">ชื่อนักเรียน</th>
                <th class="px-4 py-3 border-b">รหัส</th>
                <th class="px-4 py-3 border-b">สถานที่</th>
                <th class="px-4 py-3 border-b">จำนวนคนที่ไป</th>
                <th class="px-4 py-3 border-b">โหวตเมื่อ</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100">
              <tr v-for="f in filteredFamilies" :key="f.studentId" class="hover:bg-slate-50">
                <td class="px-4 py-3">{{ roomText(f) }}</td>
                <td class="px-4 py-3 font-bold text-slate-800">{{ f.name || '-' }}</td>
                <td class="px-4 py-3 text-xs text-gray-500">{{ f.studentId }}</td>
                <td class="px-4 py-3">
                  <span v-if="f.hasVoted">{{ venueName(f.venueId) }}</span>
                  <span v-else class="text-gray-400">ยังไม่โหวต</span>
                </td>
                <td class="px-4 py-3 font-medium">{{ f.hasVoted ? peopleText(f) : '-' }}</td>
                <td class="px-4 py-3 text-xs text-gray-500">{{ votedAtText(f) }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <p v-if="!familiesError && filteredFamilies.length === 0" class="text-center text-gray-400 py-6">ไม่พบครอบครัวตามเงื่อนไข</p>
      </div>
    </template>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue';
import { fetchVenueVoteSummary, fetchVenueVoteFamilies } from '../../services/backofficeApi';
import { UnauthorizedError } from '../../services/apiClient';
import { useLiff } from '../../composables/useLiff';

const { getAccessToken, login } = useLiff();

const loading = ref(true);
const error = ref('');
const summary = ref(null);
const families = ref([]);
const familiesError = ref('');
const statusFilter = ref('all');
const venueFilter = ref('');
const search = ref('');

const statusOptions = [
  { value: 'all', label: 'ทั้งหมด' },
  { value: 'voted', label: 'โหวตแล้ว' },
  { value: 'notVoted', label: 'ยังไม่โหวต' }
];

const venueName = (id) => summary.value?.venues.find((v) => v.id === id)?.name ?? id;
const roomText = (f) => [f.newRoom, f.newNo != null ? `เลขที่ ${f.newNo}` : ''].filter(Boolean).join(' / ') || '-';
// Votes cast before the head-count field existed have no number: say so, do not invent one.
const peopleText = (f) => (f.attendeeCount == null ? 'ไม่ระบุ' : `${f.attendeeCount} คน`);
const votedAtText = (f) => (f.votedAtUtc
  ? new Date(f.votedAtUtc).toLocaleString('th-TH', { timeZone: 'Asia/Bangkok', dateStyle: 'medium', timeStyle: 'short' })
  : '-');

const filteredFamilies = computed(() => {
  const q = search.value.trim().toLowerCase();
  return families.value.filter((f) => {
    if (statusFilter.value === 'voted' && !f.hasVoted) return false;
    if (statusFilter.value === 'notVoted' && f.hasVoted) return false;
    if (venueFilter.value && f.venueId !== venueFilter.value) return false;
    if (!q) return true;
    return [f.name, f.studentId, f.newRoom].some((v) => (v || '').toLowerCase().includes(q));
  });
});

const filteredPeople = computed(() => filteredFamilies.value.reduce((sum, f) => sum + (f.attendeeCount ?? 0), 0));

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
    try {
      families.value = await fetchVenueVoteFamilies(token);
    } catch (err) {
      if (err instanceof UnauthorizedError) return;
      familiesError.value = 'โหลดรายครอบครัวไม่สำเร็จ';
    }
  } catch (err) {
    if (err instanceof UnauthorizedError) return;
    error.value = err.status === 403 ? 'ไม่มีสิทธิ์เข้าถึงส่วนนี้' : 'เกิดข้อผิดพลาดในการโหลดข้อมูล';
  } finally {
    loading.value = false;
  }
});
</script>
