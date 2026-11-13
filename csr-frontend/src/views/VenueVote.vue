<template>
  <div class="min-h-screen bg-gray-50">
    <FrontofficePageHeader title="โหวตสถานที่จัดกิจกรรม" show-back show-home />

    <main class="mx-auto max-w-2xl px-4 py-6 pb-16 space-y-5">
      <div v-if="isLoading" class="py-10 text-center text-gray-500 animate-pulse">กำลังโหลดข้อมูล...</div>

      <div v-else-if="loadError" class="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
        <p>{{ loadError }}</p>
        <button type="button" class="mt-3 w-full rounded-lg bg-red-600 py-2 font-bold text-white transition active:scale-95" @click="load">
          ลองใหม่
        </button>
      </div>

      <template v-else-if="state">
        <section class="rounded-2xl border border-border bg-surface p-5 shadow-sm">
          <h1 class="text-lg font-bold text-text-primary">เลือกสถานที่จัดกิจกรรม</h1>
          <p class="mt-1 text-sm text-text-secondary">1 ครอบครัว โหวตได้ 1 สถานที่</p>
          <p v-if="state.isClosed" class="mt-2 text-sm font-bold text-red-600">ปิดโหวตแล้ว</p>
          <p v-else-if="closesAtText" class="mt-2 text-sm text-text-secondary">
            ปิดโหวต {{ closesAtText }} เปลี่ยนโหวตได้จนถึงเวลาปิด
          </p>
        </section>

        <div v-if="!state.isRegistered" class="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
          <p class="font-bold">ยังไม่ได้ลงทะเบียนนักเรียน</p>
          <p class="mt-1">ลงทะเบียนก่อนจึงจะโหวตได้</p>
          <router-link to="/register" class="mt-3 block w-full rounded-lg bg-brand-primary py-2 text-center font-bold text-white transition active:scale-95">
            ลงทะเบียนนักเรียน
          </router-link>
        </div>

        <p v-if="savedMessage" role="status" class="rounded-xl border border-green-200 bg-green-50 p-3 text-center text-sm font-bold text-green-800">
          {{ savedMessage }}
        </p>

        <fieldset class="space-y-3" :disabled="!canVote">
          <legend class="sr-only">สถานที่</legend>
          <div
            v-for="venue in state.venues"
            :key="venue.id"
            class="rounded-2xl border-2 bg-surface p-4 shadow-sm transition"
            :class="selectedVenueId === venue.id ? 'border-brand-primary bg-brand-primary-soft' : 'border-border'"
          >
            <label class="flex items-start gap-3" :class="canVote ? 'cursor-pointer' : ''">
              <input
                v-model="selectedVenueId"
                type="radio"
                name="venue"
                :value="venue.id"
                class="mt-1 h-5 w-5 shrink-0 accent-brand-primary"
              />
              <span class="min-w-0 flex-1">
                <span class="block font-bold text-text-primary">{{ venue.name }}</span>
                <span class="block text-sm text-text-secondary">{{ venue.area }}</span>
                <span v-if="state.myVenueId === venue.id" class="mt-1 inline-block rounded-full bg-brand-primary px-2 py-0.5 text-xs font-bold text-white">
                  โหวตของคุณ
                </span>
              </span>
            </label>

            <div v-if="state.counts" class="mt-3">
              <div class="flex items-center justify-between text-xs text-text-secondary">
                <span>{{ countOf(venue.id) }} โหวต · {{ peopleOf(venue.id) }} คน</span>
                <span>{{ percentOf(venue.id) }}%</span>
              </div>
              <div class="mt-1 h-2 overflow-hidden rounded-full bg-gray-200">
                <div class="h-full rounded-full bg-brand-primary transition-all" :style="{ width: `${percentOf(venue.id)}%` }"></div>
              </div>
            </div>

            <a
              :href="venue.url"
              target="_blank"
              rel="noopener noreferrer"
              class="mt-3 inline-block text-sm font-bold text-action-primary underline"
            >
              ดูรายละเอียดสถานที่
            </a>
          </div>
        </fieldset>

        <!-- Attendee count: required before the vote can be sent -->
        <section v-if="canVote" class="rounded-2xl border border-border bg-surface p-5 shadow-sm">
          <label for="attendee-count" class="block font-bold text-text-primary">จำนวนคนที่จะไป (รวมตัวเอง)</label>
          <p class="mt-1 text-sm text-text-secondary">ใส่ {{ minAttendees }}–{{ state.maxAttendees }} คน ก่อนกดยืนยันโหวต</p>
          <div class="mt-3 flex items-center justify-center gap-3">
            <button
              type="button"
              class="h-12 w-12 rounded-xl border border-border bg-surface text-2xl font-bold text-text-primary transition active:scale-95 disabled:opacity-40"
              aria-label="ลดจำนวนคน"
              :disabled="isSubmitting || !(attendeeCount > minAttendees)"
              @click="stepAttendees(-1)"
            >−</button>
            <input
              id="attendee-count"
              v-model="attendeeInput"
              type="number"
              inputmode="numeric"
              :min="minAttendees"
              :max="state.maxAttendees"
              step="1"
              placeholder="0"
              class="h-12 w-24 rounded-xl border-2 text-center text-xl font-bold text-text-primary focus:outline-none focus:ring-4 focus:ring-focus-ring"
              :class="attendeeError ? 'border-red-400' : 'border-border'"
              :aria-invalid="attendeeError ? 'true' : 'false'"
            />
            <button
              type="button"
              class="h-12 w-12 rounded-xl border border-border bg-surface text-2xl font-bold text-text-primary transition active:scale-95 disabled:opacity-40"
              aria-label="เพิ่มจำนวนคน"
              :disabled="isSubmitting || (attendeeCount !== null && attendeeCount >= state.maxAttendees)"
              @click="stepAttendees(1)"
            >+</button>
          </div>
          <p v-if="attendeeError" role="alert" class="mt-2 text-center text-sm text-red-500">{{ attendeeError }}</p>
        </section>

        <p v-if="state.counts" class="text-center text-sm text-text-secondary">
          โหวตแล้วทั้งหมด {{ state.totalVotes }} ครอบครัว · รวม {{ state.totalPeople }} คน
        </p>
        <p v-else-if="canVote" class="text-center text-sm text-text-secondary">
          ผลโหวตจะแสดงหลังจากคุณโหวตแล้ว
        </p>

        <button
          v-if="canVote"
          type="button"
          class="block w-full rounded-xl bg-brand-primary px-4 py-3 text-center font-bold text-white shadow-sm transition hover:bg-brand-primary-strong focus:ring-4 focus:ring-focus-ring active:scale-95 disabled:cursor-not-allowed disabled:opacity-50"
          :disabled="!canSubmit"
          @click="submit"
        >
          {{ isSubmitting ? 'กำลังบันทึก...' : state.myVenueId ? 'เปลี่ยนโหวต' : 'ยืนยันโหวต' }}
        </button>
      </template>
    </main>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue';
import FrontofficePageHeader from '../components/FrontofficePageHeader.vue';
import { useLiff } from '../composables/useLiff';
import { fetchVenueVote, submitVenueVote } from '../services/venueVoteApi';
import { UnauthorizedError } from '../services/apiClient';

const { getAccessToken, login } = useLiff();

const minAttendees = 1;

const state = ref(null);
const selectedVenueId = ref(null);
// Raw input text ('' when empty). Kept as typed so a half-entered value is not rewritten under the user.
const attendeeInput = ref('');
const isLoading = ref(true);
const isSubmitting = ref(false);
const loadError = ref('');
const savedMessage = ref('');

const canVote = computed(() => !!state.value && state.value.isRegistered && !state.value.isClosed);

// null = empty or not a whole number
const attendeeCount = computed(() => {
  const text = String(attendeeInput.value ?? '').trim();
  if (!/^\d+$/.test(text)) return null;
  return Number(text);
});

const isAttendeeValid = computed(() =>
  attendeeCount.value !== null &&
  attendeeCount.value >= minAttendees &&
  attendeeCount.value <= (state.value?.maxAttendees ?? minAttendees)
);

// Only complain once the user has typed something; an empty field just keeps the button off.
const attendeeError = computed(() => {
  const text = String(attendeeInput.value ?? '').trim();
  if (!text || isAttendeeValid.value) return '';
  return `กรุณาใส่จำนวนเต็ม ${minAttendees}–${state.value.maxAttendees} คน`;
});

const hasChanges = computed(() =>
  selectedVenueId.value !== state.value?.myVenueId ||
  attendeeCount.value !== (state.value?.myAttendeeCount ?? null)
);

const canSubmit = computed(() =>
  canVote.value &&
  !isSubmitting.value &&
  !!selectedVenueId.value &&
  isAttendeeValid.value &&
  hasChanges.value
);

const closesAtText = computed(() => {
  if (!state.value?.closesAtUtc) return '';
  return new Date(state.value.closesAtUtc).toLocaleString('th-TH', {
    timeZone: 'Asia/Bangkok',
    dateStyle: 'long',
    timeStyle: 'short'
  });
});

function countOf(venueId) {
  return state.value?.counts?.find((c) => c.venueId === venueId)?.count ?? 0;
}

function peopleOf(venueId) {
  return state.value?.counts?.find((c) => c.venueId === venueId)?.people ?? 0;
}

function percentOf(venueId) {
  const total = state.value?.totalVotes ?? 0;
  return total > 0 ? Math.round((countOf(venueId) / total) * 100) : 0;
}

function stepAttendees(delta) {
  const next = (attendeeCount.value ?? 0) + delta;
  attendeeInput.value = String(Math.min(Math.max(next, minAttendees), state.value.maxAttendees));
}

function applyState(next) {
  state.value = next;
  selectedVenueId.value = next.myVenueId ?? null;
  // Votes cast before the count existed have none: leave the field empty so the family fills it in.
  attendeeInput.value = next.myAttendeeCount != null ? String(next.myAttendeeCount) : '';
}

async function load() {
  const token = getAccessToken();
  if (!token) {
    login();
    return;
  }

  isLoading.value = true;
  loadError.value = '';
  try {
    applyState(await fetchVenueVote(token));
  } catch (error) {
    if (error instanceof UnauthorizedError) return; // redirectToLogin จัดการแล้ว
    console.error('[VenueVote] load failed:', error);
    loadError.value = 'โหลดข้อมูลไม่สำเร็จ กรุณาลองใหม่อีกครั้ง';
  } finally {
    isLoading.value = false;
  }
}

async function submit() {
  if (!canSubmit.value) return;

  const token = getAccessToken();
  if (!token) {
    login();
    return;
  }

  isSubmitting.value = true;
  savedMessage.value = '';
  try {
    applyState(await submitVenueVote(token, selectedVenueId.value, attendeeCount.value));
    savedMessage.value = 'บันทึกโหวตแล้ว';
  } catch (error) {
    if (error instanceof UnauthorizedError) return; // redirectToLogin จัดการแล้ว
    console.error('[VenueVote] submit failed:', error);
    alert(error.status && error.status < 500 && error.message ? error.message : 'บันทึกโหวตไม่สำเร็จ กรุณาลองใหม่อีกครั้ง');
    if (error.status === 403 || (error.status === 400 && error.message.includes('ปิดโหวต'))) await load(); // e.g. vote closed meanwhile
  } finally {
    isSubmitting.value = false;
  }
}

onMounted(load);
</script>
