<template>
  <div class="min-h-screen bg-gray-50">
    <FrontofficePageHeader title="เข้าร่วมในฐานะผู้ปกครอง" show-back show-home />

    <main class="mx-auto max-w-2xl px-4 py-6 pb-16 space-y-5">
      <section class="rounded-2xl border border-border bg-surface p-5 shadow-sm">
        <h1 class="text-lg font-bold text-text-primary">พ่อ/แม่ลงทะเบียนนักเรียนไว้แล้ว?</h1>
        <p class="mt-1 text-sm text-text-secondary">
          ผู้ปกครองอีกคนที่ใช้ LINE คนละบัญชี กรอกรหัสนักเรียนกับเบอร์โทรนักเรียนเพื่อเข้าร่วม
          จะเห็นข้อมูลและโหวตเดียวกับครอบครัวเดิม ไม่ต้องลงทะเบียนซ้ำ
        </p>
      </section>

      <form class="space-y-4 rounded-2xl border border-border bg-surface p-5 shadow-sm" novalidate @submit.prevent="submit">
        <div>
          <label for="join-student-id" class="block text-sm font-bold text-text-primary">รหัสประจำตัวนักเรียน</label>
          <input
            id="join-student-id"
            v-model="studentId"
            type="text"
            inputmode="numeric"
            autocomplete="off"
            placeholder="เช่น 30558"
            class="mt-1 block w-full rounded-md border border-border p-2 shadow-sm focus:outline-none focus:ring-4"
          />
        </div>

        <div>
          <label for="join-phone" class="block text-sm font-bold text-text-primary">เบอร์โทรนักเรียน</label>
          <input
            id="join-phone"
            v-model="phone"
            type="tel"
            autocomplete="off"
            placeholder="08..."
            class="mt-1 block w-full rounded-md border border-border p-2 shadow-sm focus:outline-none focus:ring-4"
          />
          <p class="mt-1 text-xs text-text-secondary">เบอร์เดียวกับที่กรอกในช่อง "เบอร์โทรนักเรียน" ตอนลงทะเบียน</p>
        </div>

        <p v-if="errorMessage" role="alert" class="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          {{ errorMessage }}
        </p>

        <button
          type="submit"
          :disabled="!canSubmit"
          class="w-full rounded-xl bg-brand-primary py-3 font-bold text-white transition active:scale-95 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {{ isSubmitting ? 'กำลังตรวจสอบ...' : 'เข้าร่วมด้วย LINE นี้' }}
        </button>
      </form>

      <p class="text-center text-sm text-text-secondary">
        ยังไม่เคยมีใครลงทะเบียนนักเรียนคนนี้?
        <router-link to="/register" class="font-bold text-action-primary underline">ลงทะเบียนนักเรียน</router-link>
      </p>
    </main>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';
import FrontofficePageHeader from '../components/FrontofficePageHeader.vue';
import { useLiff } from '../composables/useLiff';
import { joinAsGuardian } from '../services/registrationApi';
import { UnauthorizedError } from '../services/apiClient';

const router = useRouter();
const { getAccessToken, login } = useLiff();

const studentId = ref('');
const phone = ref('');
const isSubmitting = ref(false);
const errorMessage = ref('');

const canSubmit = computed(() =>
  !isSubmitting.value && studentId.value.trim() !== '' && phone.value.replace(/\D/g, '').length >= 9
);

async function submit() {
  if (!canSubmit.value) return;

  const token = getAccessToken();
  if (!token) {
    login();
    return;
  }

  isSubmitting.value = true;
  errorMessage.value = '';
  try {
    await joinAsGuardian(token, studentId.value.trim(), phone.value);
    alert('เข้าร่วมสำเร็จ ตอนนี้ใช้งานในฐานะผู้ปกครองของนักเรียนคนนี้ได้แล้ว');
    router.push('/');
  } catch (error) {
    if (error instanceof UnauthorizedError) return; // redirectToLogin จัดการแล้ว
    console.error('[JoinGuardian] failed:', error);
    errorMessage.value = error.status && error.status < 500 && error.message
      ? error.message
      : 'เข้าร่วมไม่สำเร็จ กรุณาลองใหม่อีกครั้ง';
  } finally {
    isSubmitting.value = false;
  }
}
</script>
