<template>
  <div class="min-h-screen bg-gray-50">
    <FrontofficePageHeader title="สั่งซื้อเสื้อนักเรียน" show-back show-home />

    <main class="mx-auto max-w-2xl px-4 py-6 pb-32">
      <!-- Success State -->
      <div v-if="isSuccess" class="space-y-6">
        <div class="rounded-2xl bg-green-50 p-8 text-center border border-green-200">
          <div class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-green-100">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
            </svg>
          </div>
          <h2 class="text-xl font-bold text-green-800">สั่งซื้อสำเร็จ!</h2>
          <p v-if="orderResult?.orderId" class="mt-2 text-sm text-green-700">
            หมายเลขคำสั่งซื้อ: <span class="font-mono font-bold">{{ orderResult.orderId }}</span>
          </p>
        </div>

        <div class="rounded-2xl border border-border bg-surface p-5 shadow-sm space-y-4">
          <h3 class="font-bold text-text-primary">สรุปคำสั่งซื้อ</h3>
          <div class="rounded-xl bg-brand-primary-soft p-4">
            <p class="text-sm text-brand-primary-strong">{{ orderSummary }}</p>
            <p class="mt-1 text-lg font-bold text-brand-primary-strong">฿{{ totalAmount.toLocaleString() }}</p>
          </div>

          <div class="space-y-2 text-sm text-text-secondary">
            <p><span class="font-medium text-text-primary">นักเรียน:</span> {{ form.studentName }}</p>
            <p><span class="font-medium text-text-primary">เลขที่:</span> {{ form.studentNumber }}</p>
          </div>

          <div class="flex items-center gap-2 text-sm text-green-700">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
            </svg>
            <span>สลิปแนบเรียบร้อย</span>
          </div>
        </div>

        <router-link to="/dashboard" class="block w-full rounded-xl bg-brand-primary px-4 py-3 text-center font-bold text-white shadow-sm transition hover:bg-brand-primary-strong focus:ring-4 focus:ring-focus-ring active:scale-95">
          🏠 กลับสู่หน้าหลัก
        </router-link>
      </div>

      <!-- Order Form -->
      <div v-else class="space-y-8">
        <!-- Shirt Designs -->
        <section>
          <h2 class="mb-3 text-sm font-bold uppercase tracking-wider text-text-secondary">รูปแบบเสื้อ</h2>
          <div class="grid grid-cols-2 gap-4">
            <div class="rounded-2xl border border-border bg-surface p-3 shadow-sm text-center">
              <div class="aspect-square w-full rounded-xl bg-gray-100 flex items-center justify-center mb-2 overflow-hidden">
                <!-- Replace src with actual image path -->
                <img :src="shirtImageA" alt="เสื้อแบบ A" class="h-full w-full object-cover" loading="lazy" @error="onImageError" />
              </div>
              <p class="font-bold text-text-primary">แบบ A</p>
              <p class="text-xs text-text-secondary">฿250/ตัว</p>
            </div>
            <div class="rounded-2xl border border-border bg-surface p-3 shadow-sm text-center">
              <div class="aspect-square w-full rounded-xl bg-gray-100 flex items-center justify-center mb-2 overflow-hidden">
                <img :src="shirtImageB" alt="เสื้อแบบ B" class="h-full w-full object-cover" loading="lazy" @error="onImageError" />
              </div>
              <p class="font-bold text-text-primary">แบบ B</p>
              <p class="text-xs text-text-secondary">฿250/ตัว</p>
            </div>
          </div>
          <p class="mt-2 text-xs text-gray-500">* แทนที่ไฟล์รูปในโฟลเดอร์ <code>public/images/</code></p>
        </section>

        <!-- Size Chart -->
        <section>
          <h2 class="mb-3 text-sm font-bold uppercase tracking-wider text-text-secondary">ตารางขนาดเสื้อ</h2>
          <div class="overflow-hidden rounded-xl border border-border bg-surface shadow-sm">
            <table class="w-full text-sm">
              <thead>
                <tr class="bg-gray-100 text-text-secondary">
                  <th class="px-3 py-2 text-left font-semibold">ไซส์</th>
                  <th class="px-3 py-2 text-center font-semibold">รอบอก (นิ้ว)</th>
                  <th class="px-3 py-2 text-center font-semibold">ความยาว (นิ้ว)</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-border">
                <tr v-for="row in sizeChart" :key="row.size" class="hover:bg-gray-50">
                  <td class="px-3 py-2 font-medium text-text-primary">{{ row.size }}</td>
                  <td class="px-3 py-2 text-center text-text-secondary">{{ row.chest }}</td>
                  <td class="px-3 py-2 text-center text-text-secondary">{{ row.length }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <!-- Quantity Selection -->
        <section>
          <h2 class="mb-3 text-sm font-bold uppercase tracking-wider text-text-secondary">เลือกจำนวน</h2>

          <div v-for="design in DESIGNS" :key="design" class="mb-6">
            <div class="mb-2 flex items-center gap-2">
              <span class="font-bold text-text-primary">🎽 แบบ {{ design }}</span>
              <span class="rounded-full bg-brand-primary-soft px-2 py-0.5 text-xs font-semibold text-brand-primary-strong">฿{{ UNIT_PRICE }}/ตัว</span>
            </div>
            <div class="grid grid-cols-3 gap-2">
              <div v-for="size in SIZES" :key="`${design}-${size}`" class="rounded-xl border border-border bg-surface p-2 text-center shadow-sm">
                <p class="text-xs font-bold text-text-secondary">{{ size }}</p>
                <div class="mt-1 flex items-center justify-center gap-1">
                  <button
                    type="button"
                    class="flex h-7 w-7 items-center justify-center rounded-lg bg-gray-100 text-gray-600 transition hover:bg-gray-200 active:scale-95"
                    @click="decrement(design, size)"
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                      <path fill-rule="evenodd" d="M3 10a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1z" clip-rule="evenodd" />
                    </svg>
                  </button>
                  <span class="min-w-[1.5rem] text-center text-sm font-bold text-text-primary">
                    {{ getQuantity(design, size) }}
                  </span>
                  <button
                    type="button"
                    class="flex h-7 w-7 items-center justify-center rounded-lg bg-brand-primary text-white transition hover:bg-brand-primary-strong active:scale-95"
                    @click="increment(design, size)"
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                      <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
                    </svg>
                  </button>
                </div>
              </div>
            </div>
          </div>

          <p v-if="errors.items" class="mt-2 text-sm text-red-500">{{ errors.items }}</p>
        </section>

        <!-- Sticky Total -->
        <div class="sticky bottom-4 z-10">
          <div class="rounded-2xl border border-brand-primary bg-white p-4 shadow-lg">
            <div class="flex items-center justify-between">
              <div>
                <p class="text-xs text-text-secondary">รวมทั้งสิ้น</p>
                <p class="text-2xl font-bold text-brand-primary-strong">฿{{ totalAmount.toLocaleString() }}</p>
              </div>
              <div v-if="orderSummary" class="text-right text-xs text-text-secondary max-w-[60%]">
                {{ orderSummary }}
              </div>
            </div>
            <p v-if="totalQuantity > 0" class="mt-1 text-xs text-brand-primary-strong">
              จำนวน {{ totalQuantity }} ตัว
            </p>
          </div>
        </div>

        <!-- Student Info -->
        <section>
          <h2 class="mb-3 text-sm font-bold uppercase tracking-wider text-text-secondary">ข้อมูลนักเรียน</h2>
          <div class="space-y-4 rounded-2xl border border-border bg-surface p-5 shadow-sm">
            <div>
              <label class="mb-1 block text-sm font-medium text-text-primary">
                ชื่อ-นามสกุล <span class="text-red-500">*</span>
              </label>
              <input
                v-model="form.studentName"
                type="text"
                placeholder="ชื่อ-นามสกุลนักเรียน"
                class="w-full rounded-xl border border-border bg-white px-4 py-3 text-sm text-text-primary shadow-sm transition focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-primary-soft"
              />
              <p v-if="errors.studentName" class="mt-1 text-sm text-red-500">{{ errors.studentName }}</p>
            </div>

            <div>
              <label class="mb-1 block text-sm font-medium text-text-primary">
                เลขที่ <span class="text-red-500">*</span>
              </label>
              <input
                v-model="form.studentNumber"
                type="text"
                placeholder="เลขที่"
                class="w-full rounded-xl border border-border bg-white px-4 py-3 text-sm text-text-primary shadow-sm transition focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-primary-soft"
              />
              <p v-if="errors.studentNumber" class="mt-1 text-sm text-red-500">{{ errors.studentNumber }}</p>
            </div>
          </div>
        </section>

        <!-- Payment -->
        <section>
          <h2 class="mb-3 text-sm font-bold uppercase tracking-wider text-text-secondary">ชำระเงิน</h2>

          <!-- Bank Info -->
          <div class="mb-4 rounded-2xl border border-border bg-surface p-5 shadow-sm">
            <div class="flex items-center gap-2 mb-3">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 text-brand-primary" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M4 4a2 2 0 00-2 2v4a2 2 0 002 2V6h10a2 2 0 00-2-2H4zm2 6a2 2 0 012-2h8a2 2 0 012 2v4a2 2 0 01-2 2H8a2 2 0 01-2-2v-4zm6 4a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
              </svg>
              <span class="font-bold text-text-primary">บัญชีธนาคารสำหรับโอน</span>
            </div>
            <div class="rounded-xl bg-gray-50 p-4 space-y-2 text-sm">
              <p class="flex justify-between">
                <span class="text-text-secondary">ชื่อบัญชี:</span>
                <span class="font-medium text-text-primary">โรงเรียนบ้าน CSR</span>
              </p>
              <p class="flex justify-between">
                <span class="text-text-secondary">เลขบัญชี:</span>
                <span class="font-mono font-medium text-text-primary">123-4-56789-0</span>
              </p>
              <p class="flex justify-between">
                <span class="text-text-secondary">ธนาคาร:</span>
                <span class="font-medium text-text-primary">ไทยพาณิชย์</span>
              </p>
            </div>
            <p class="mt-2 text-xs text-gray-500">* แก้ไขข้อมูลบัญชีได้ที่ไฟล์นี้ (ค้นหา "โรงเรียนบ้าน CSR")</p>
          </div>

          <!-- Slip Upload -->
          <div class="rounded-2xl border border-border bg-surface p-5 shadow-sm">
            <label class="mb-1 block text-sm font-medium text-text-primary">
              แนบสลิปการโอนเงิน <span class="text-red-500">*</span>
            </label>
            <p class="mb-3 text-xs text-text-secondary">รองรับ JPG, PNG (สูงสุด 5MB)</p>

            <label class="flex cursor-pointer items-center justify-center gap-2 rounded-xl border-2 border-dashed border-border bg-gray-50 px-4 py-6 transition hover:border-brand-primary hover:bg-brand-primary-soft/30">
              <input type="file" accept="image/jpeg,image/png" class="hidden" @change="onSlipChange" />
              <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 text-brand-primary" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
              <span class="text-sm font-medium text-brand-primary">{{ slipFile ? 'เปลี่ยนสลิป' : 'อัปโหลดสลิป' }}</span>
            </label>

            <p v-if="slipFile" class="mt-2 text-sm text-text-secondary">
              ไฟล์: <span class="font-medium text-text-primary">{{ slipFile.name }}</span>
              <button type="button" class="ml-2 text-xs text-red-500 underline" @click="setSlipFile(null)">ลบ</button>
            </p>

            <div v-if="slipPreviewUrl" class="mt-3 overflow-hidden rounded-xl border border-border">
              <img :src="slipPreviewUrl" alt="Preview สลิป" class="w-full object-contain" />
            </div>

            <p v-if="errors.slip" class="mt-2 text-sm text-red-500">{{ errors.slip }}</p>
          </div>
        </section>

        <!-- Submit -->
        <div class="pt-4">
          <button
            type="button"
            class="w-full rounded-xl bg-brand-primary px-4 py-4 text-center text-lg font-bold text-white shadow-lg transition hover:bg-brand-primary-strong focus:ring-4 focus:ring-focus-ring active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed"
            :disabled="!canSubmit || isSubmitting"
            @click="submitOrder"
          >
            <span v-if="isSubmitting">กำลังส่งคำสั่งซื้อ...</span>
            <span v-else>✅ ยืนยันคำสั่งซื้อ</span>
          </button>
          <p v-if="!canSubmit && !isSubmitting" class="mt-2 text-center text-xs text-gray-500">
            กรุณากรอกข้อมูลให้ครบถ้วนและเลือกเสื้ออย่างน้อย 1 ตัว
          </p>
        </div>
      </div>
    </main>
  </div>
</template>

<script setup>
import { computed, onMounted, onBeforeUnmount, ref } from 'vue';
import FrontofficePageHeader from '../components/FrontofficePageHeader.vue';
import { useShirtOrder } from '../composables/useShirtOrder';
import { useLiff } from '../composables/useLiff';
import { submitShirtOrder } from '../services/shirtOrderApi';

const shirtImageA = ref('/images/shirt-a.svg');
const shirtImageB = ref('/images/shirt-b.svg');

function onImageError(event) {
  const el = event.target;
  el.style.display = 'none';
  const parent = el.parentElement;
  if (parent) {
    parent.innerHTML = el.alt;
    parent.classList.add('text-4xl');
  }
}

const {
  form,
  errors,
  slipFile,
  slipPreviewUrl,
  isSubmitting,
  isSuccess,
  orderResult,
  totalAmount,
  totalQuantity,
  orderSummary,
  SIZES,
  DESIGNS,
  UNIT_PRICE,
  setQuantity,
  increment,
  decrement,
  setSlipFile,
  validateForm,
  resetForm
} = useShirtOrder();

const { getAccessToken, profile } = useLiff();

const sizeChart = [
  { size: 'XS', chest: '32-34', length: '26' },
  { size: 'S', chest: '34-36', length: '27' },
  { size: 'M', chest: '36-38', length: '28' },
  { size: 'L', chest: '38-40', length: '29' },
  { size: 'XL', chest: '40-42', length: '30' },
  { size: '2XL', chest: '42-44', length: '31' },
  { size: '3XL', chest: '44-46', length: '32' },
  { size: '4XL', chest: '46-48', length: '33' },
  { size: '5XL', chest: '48-50', length: '34' },
  { size: '6XL', chest: '50-52', length: '35' },
  { size: '7XL', chest: '52-54', length: '36' }
];

const canSubmit = computed(() => {
  return form.studentName?.trim().length > 0 &&
    form.studentNumber?.trim().length > 0 &&
    totalQuantity.value > 0 &&
    slipFile.value !== null;
});

function getQuantity(design, size) {
  const item = form.items.find(i => i.design === design && i.size === size);
  return item?.quantity || 0;
}

function onSlipChange(event) {
  const [file] = event.target.files || [];
  if (file && file.size > 5 * 1024 * 1024) {
    alert('ไฟล์ใหญ่เกิน 5MB');
    event.target.value = '';
    return;
  }
  setSlipFile(file ?? null);
  event.target.value = '';
}

async function submitOrder() {
  if (isSubmitting.value) return;

  if (!validateForm()) {
    // Scroll to first error
    const firstError = document.querySelector('.text-red-500');
    firstError?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    return;
  }

  const token = getAccessToken();
  if (!token) {
    alert('กรุณาเข้าสู่ระบบ LINE ก่อน');
    return;
  }

  isSubmitting.value = true;

  try {
    // Pre-fill LINE display name if available
    if (!form.lineDisplayName && profile.value?.displayName) {
      form.lineDisplayName = profile.value.displayName;
    }

    const result = await submitShirtOrder(token, form, slipFile.value, totalAmount.value, orderSummary.value);
    orderResult.value = result;
    isSuccess.value = true;
  } catch (error) {
    console.error('Order submission failed:', error);
    alert(error.message || 'สั่งซื้อไม่สำเร็จ กรุณาลองใหม่อีกครั้ง');
  } finally {
    isSubmitting.value = false;
  }
}

onMounted(() => {
  // Pre-fill LINE display name from profile
  if (profile.value?.displayName) {
    form.lineDisplayName = profile.value.displayName;
  }
});

onBeforeUnmount(() => {
  if (slipPreviewUrl.value) {
    URL.revokeObjectURL(slipPreviewUrl.value);
  }
});
</script>
