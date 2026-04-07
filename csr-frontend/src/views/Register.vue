<template>
  <div>
    <FrontofficePageHeader :title="pageTitle" />

    <main class="p-4 space-y-6">
    <h1 class="bg-gradient-to-r from-brand-primary-strong to-brand-secondary-strong bg-clip-text text-center text-xl font-bold text-transparent">{{ formTitle }}</h1>

    <div v-if="!isReady" class="py-10 text-center text-text-secondary animate-pulse">
      กำลังเชื่อมต่อ LINE...
    </div>

    <form v-else @submit.prevent="submitForm" novalidate class="space-y-6">
      <!-- Student Info -->
      <section class="rounded-xl border border-border bg-surface p-4 shadow-sm">
        <h2 class="mb-3 border-b border-border pb-2 font-bold text-text-primary">👤 ข้อมูลนักเรียน</h2>

        <div class="space-y-3">
          <div>
            <FieldLabel :required="true">รหัสประจำตัว (Student ID)</FieldLabel>
            <input v-model="form.student.studentId" type="text" class="mt-1 block w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass(errors.student.studentId)" placeholder="เช่น 30558" />
            <p v-if="errors.student.studentId" class="text-red-500 text-xs mt-1">{{ errors.student.studentId }}</p>
          </div>

          <div>
            <FieldLabel :required="true">ชื่อ</FieldLabel>
            <input v-model="form.student.firstName" type="text" class="mt-1 block w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass(errors.student.firstName)" placeholder="เด็กชาย..." />
            <p v-if="errors.student.firstName" class="text-red-500 text-xs mt-1">{{ errors.student.firstName }}</p>
          </div>

          <div>
            <FieldLabel :required="true">นามสกุล</FieldLabel>
            <input v-model="form.student.lastName" type="text" class="mt-1 block w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass(errors.student.lastName)" placeholder="นามสกุล" />
            <p v-if="errors.student.lastName" class="text-red-500 text-xs mt-1">{{ errors.student.lastName }}</p>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-sm font-medium text-text-secondary">ชื่อเล่น</label>
              <input v-model="form.student.nickname" type="text" class="mt-1 block w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass()" placeholder="ชื่อเล่น" />
            </div>
            <div>
              <FieldLabel :required="true">เลขที่</FieldLabel>
              <input v-model.number="form.student.newNo" type="number" min="1" max="50" class="mt-1 block w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass(errors.student.newNo)" placeholder="1" />
              <p v-if="errors.student.newNo" class="text-red-500 text-xs mt-1">{{ errors.student.newNo }}</p>
            </div>
          </div>

          <div>
            <label class="block text-sm font-medium text-text-secondary">เบอร์โทรศัพท์นักเรียน (ถ้ามี)</label>
            <input v-model="form.student.phone" type="tel" class="mt-1 block w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass()" placeholder="08..." />
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-sm font-medium text-text-secondary">กรุ๊ปเลือด</label>
              <select v-model="form.student.bloodType" class="mt-1 block h-[42px] w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass()">
                <option value="">-- เลือก --</option>
                <option value="A">A</option>
                <option value="B">B</option>
                <option value="O">O</option>
                <option value="AB">AB</option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-medium text-text-secondary">วันเกิด</label>
              <VueDatePicker
                v-model="form.student.dob"
                class="mt-1 w-full"
                :time-config="{ enableTimePicker: false }"
                :model-type="'yyyy-MM-dd'"
                :formats="{ input: 'dd/MM/yyyy' }"
                auto-apply
                placeholder="dd/MM/yyyy"
              />
            </div>
          </div>
        </div>
      </section>

      <StudentPhotoUpload
        :model-value="photos.studentPhoto"
        :existing-photo-url="existingPhotoUrls.student"
        :access-token="accessToken"
        @update:model-value="setStudentPhoto"
      />

      <!-- Guardian 1 Info (Primary) -->
      <section class="rounded-xl border border-brand-primary-soft bg-brand-primary-soft/35 p-4 shadow-sm">
        <h2 class="mb-3 border-b border-brand-primary-soft pb-2 font-bold text-brand-primary-strong">🛡️ ข้อมูลผู้ปกครองคนที่ 1 (คุณ)</h2>

        <div class="space-y-3">
          <div>
            <FieldLabel :required="true" label-class="text-brand-primary-strong">ชื่อ</FieldLabel>
            <input v-model="form.guardians[0].firstName" type="text" class="mt-1 block w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass(errors.guardians[0].firstName, 'primary')" placeholder="นาย..." />
            <p v-if="errors.guardians[0].firstName" class="text-red-500 text-xs mt-1">{{ errors.guardians[0].firstName }}</p>
          </div>

          <div>
            <FieldLabel :required="true" label-class="text-brand-primary-strong">นามสกุล</FieldLabel>
            <input v-model="form.guardians[0].lastName" type="text" class="mt-1 block w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass(errors.guardians[0].lastName, 'primary')" placeholder="นามสกุล" />
            <p v-if="errors.guardians[0].lastName" class="text-red-500 text-xs mt-1">{{ errors.guardians[0].lastName }}</p>
          </div>

          <div>
            <FieldLabel :required="true" label-class="text-brand-primary-strong">ความสัมพันธ์</FieldLabel>
            <select v-model="form.guardians[0].relationType" class="mt-1 block w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass(errors.guardians[0].relationType, 'primary')">
              <option value="" disabled>-- เลือก --</option>
              <option value="Father">บิดา</option>
              <option value="Mother">มารดา</option>
              <option value="Other">ผู้ปกครองอื่นๆ</option>
            </select>
            <p v-if="errors.guardians[0].relationType" class="text-red-500 text-xs mt-1">{{ errors.guardians[0].relationType }}</p>
          </div>

          <div>
            <FieldLabel :required="true" label-class="text-brand-primary-strong">เบอร์โทรศัพท์ติดต่อ</FieldLabel>
            <input v-model="form.guardians[0].phone" type="tel" class="mt-1 block w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass(errors.guardians[0].phone, 'primary')" placeholder="08..." />
            <p v-if="errors.guardians[0].phone" class="text-red-500 text-xs mt-1">{{ errors.guardians[0].phone }}</p>
          </div>

          <div>
            <label class="block text-sm font-medium text-brand-primary-strong">อาชีพ</label>
            <input v-model="form.guardians[0].occupation" type="text" class="mt-1 block w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass(undefined, 'primary')" placeholder="ข้าราชการ, พนักงานบริษัท..." />
          </div>

          <div>
            <label class="block text-sm font-medium text-brand-primary-strong">อีเมล</label>
            <input v-model="form.guardians[0].email" type="email" class="mt-1 block w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass(errors.guardians[0].email, 'primary')" placeholder="name@example.com" />
            <p v-if="errors.guardians[0].email" class="text-red-500 text-xs mt-1">{{ errors.guardians[0].email }}</p>
          </div>
        </div>
      </section>

      <GuardianPhotoUpload
        :model-value="photos.guardianPhoto1"
        :existing-photo-url="existingPhotoUrls.guardian1"
        :access-token="accessToken"
        theme="primary"
        @update:model-value="(file) => setGuardianPhoto(file, 1)"
      />

      <!-- Guardian 2 Info (Secondary) -->
      <section class="rounded-xl border border-brand-secondary-soft bg-brand-secondary-soft/40 p-4 shadow-sm">
        <h2 class="mb-3 border-b border-brand-secondary-soft pb-2 font-bold text-brand-secondary-strong">🛡️ ข้อมูลผู้ปกครองคนที่ 2 (ถ้ามี)</h2>

        <div class="space-y-3">
          <div>
            <FieldLabel label-class="text-brand-secondary-strong">ชื่อ</FieldLabel>
            <input v-model="form.guardians[1].firstName" type="text" class="mt-1 block w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass(errors.guardians[1].firstName, 'secondary')" placeholder="นาย..." />
            <p v-if="errors.guardians[1].firstName" class="text-red-500 text-xs mt-1">{{ errors.guardians[1].firstName }}</p>
          </div>

          <div>
            <FieldLabel label-class="text-brand-secondary-strong">นามสกุล</FieldLabel>
            <input v-model="form.guardians[1].lastName" type="text" class="mt-1 block w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass(errors.guardians[1].lastName, 'secondary')" placeholder="นามสกุล" />
            <p v-if="errors.guardians[1].lastName" class="text-red-500 text-xs mt-1">{{ errors.guardians[1].lastName }}</p>
          </div>

          <div>
            <label class="block text-sm font-medium text-brand-secondary-strong">ความสัมพันธ์</label>
            <select v-model="form.guardians[1].relationType" class="mt-1 block w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass(undefined, 'secondary')">
              <option value="">-- เลือก (ถ้ามี) --</option>
              <option value="Father">บิดา</option>
              <option value="Mother">มารดา</option>
              <option value="Other">ผู้ปกครองอื่นๆ</option>
            </select>
          </div>

          <div>
            <label class="block text-sm font-medium text-brand-secondary-strong">เบอร์โทรศัพท์ติดต่อ</label>
            <input v-model="form.guardians[1].phone" type="tel" class="mt-1 block w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass(errors.guardians[1].phone, 'secondary')" placeholder="08..." />
            <p v-if="errors.guardians[1].phone" class="text-red-500 text-xs mt-1">{{ errors.guardians[1].phone }}</p>
          </div>

          <div>
            <label class="block text-sm font-medium text-brand-secondary-strong">อาชีพ</label>
            <input v-model="form.guardians[1].occupation" type="text" class="mt-1 block w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass(undefined, 'secondary')" placeholder="ข้าราชการ, พนักงานบริษัท..." />
          </div>

          <div>
            <label class="block text-sm font-medium text-brand-secondary-strong">อีเมล</label>
            <input v-model="form.guardians[1].email" type="email" class="mt-1 block w-full rounded-md border p-2 shadow-sm transition focus:ring-4 focus:outline-none" :class="inputClass(errors.guardians[1].email, 'secondary')" placeholder="name@example.com" />
            <p v-if="errors.guardians[1].email" class="text-red-500 text-xs mt-1">{{ errors.guardians[1].email }}</p>
          </div>
        </div>
      </section>

      <GuardianPhotoUpload
        :model-value="photos.guardianPhoto2"
        :existing-photo-url="existingPhotoUrls.guardian2"
        :access-token="accessToken"
        theme="secondary"
        @update:model-value="(file) => setGuardianPhoto(file, 2)"
      />

      <div v-if="isProfileLoading" class="rounded-xl border border-brand-primary-soft bg-brand-primary-soft px-4 py-3 text-sm text-brand-primary-strong">
        กำลังโหลดข้อมูลเดิมและรูปที่มีสิทธิ์เข้าถึง...
      </div>

      <button type="submit" :disabled="isSubmitting" class="w-full rounded-xl bg-action-primary px-4 py-3 font-bold text-white shadow-lg transition-all hover:bg-action-primary-hover hover:shadow-xl disabled:opacity-50">
        <span v-if="isSubmitting">พริบตาเดียว... กำลังบันทึกข้อมูล 🚀</span>
        <span v-else>{{ submitLabel }}</span>
      </button>
    </form>
    </main>
  </div>
</template>

<script setup>
import { computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useRoute } from 'vue-router';
import { useLiff } from '../composables/useLiff';
import { useRegistrationForm } from '../composables/useRegistrationForm';
import StudentPhotoUpload from '../components/StudentPhotoUpload.vue';
import GuardianPhotoUpload from '../components/GuardianPhotoUpload.vue';
import FrontofficePageHeader from '../components/FrontofficePageHeader.vue';
import FieldLabel from '../components/FieldLabel.vue';
import { VueDatePicker } from '@vuepic/vue-datepicker';
import '@vuepic/vue-datepicker/dist/main.css';

const router = useRouter();
const route = useRoute();
const { initLiff, isReady, getAccessToken } = useLiff();
const {
  form,
  errors,
  photos,
  existingPhotoUrls,
  isSubmitting,
  isProfileLoading,
  validateForm,
  loadExistingProfile: loadExistingProfileState,
  submitRegistrationForm,
  setStudentPhoto,
  setGuardianPhoto
} = useRegistrationForm();
const isEditMode = computed(() => route.name === 'EditProfile');
const pageTitle = computed(() => isEditMode.value ? 'แก้ไขข้อมูล' : 'ลงทะเบียนข้อมูล');
const formTitle = computed(() => isEditMode.value ? 'แก้ไขข้อมูลผู้ปกครองและนักเรียน' : 'ลงทะเบียนผู้ปกครองและนักเรียน');
const submitLabel = computed(() => isEditMode.value ? 'บันทึกการแก้ไขข้อมูล' : 'ลงทะเบียนข้อมูลทันที');

const accessToken = computed(() => getAccessToken());
const toneClassMap = {
  neutral: 'border-border focus:border-action-primary focus:ring-focus-ring',
  primary: 'border-brand-primary/30 focus:border-action-primary focus:ring-focus-ring',
  secondary: 'border-brand-secondary/40 focus:border-brand-secondary-strong focus:ring-brand-secondary-soft'
};

function inputClass(error, tone = 'neutral') {
  if (error) {
    return 'border-red-400 focus:border-red-500 focus:ring-red-200';
  }

  return toneClassMap[tone] || toneClassMap.neutral;
}

const loadExistingProfile = async () => {
  const token = accessToken.value;
  if (!token) {
    alert('❌ ไม่พบ LINE access token สำหรับเรียกใช้งาน API');
    router.push('/register');
    return;
  }

  try {
    await loadExistingProfileState(token);
  } catch {
    router.push('/register');
  }
};

onMounted(async () => {
  await initLiff();

  if (isEditMode.value) {
    await loadExistingProfile();
  }
});

const submitForm = async () => {
  if (isSubmitting.value) return;

  if (!validateForm()) {
    return;
  }

  try {
    const token = accessToken.value;
    if (!token) {
      alert('❌ ไม่พบ LINE access token สำหรับเรียกใช้งาน API');
      return;
    }

    await submitRegistrationForm(token);
    alert('🎉 ลงทะเบียนสำเร็จเรียบร้อย ข้อมูลเข้าสู่ระบบแล้วครับ!');
    router.push('/');
  } catch (error) {
    console.error('API Error:', error);
    alert(`⚠️ เกิดข้อผิดพลาด: ${error.message || 'ไม่สามารถติดต่อเซิร์ฟเวอร์ได้'}`);
  }
};
</script>

<style>
.dp__main {
  width: 100%;
}

.dp__input_wrap {
  width: 100%;
}

.dp__main .dp__input_wrap .dp__input {
  height: 42px;
  width: 100%;
  border-radius: 0.375rem;
  border: 1px solid var(--color-border);
  padding: 0.5rem 2.75rem 0.5rem 2.5rem;
  font-size: 0.875rem;
  color: var(--color-text-primary);
  background: var(--color-surface);
  line-height: 1.25rem;
}

.dp__main .dp__input_wrap .dp__input:focus {
  border-color: var(--color-action-primary);
  outline: none;
  box-shadow: 0 0 0 4px rgba(125, 211, 252, 0.45);
}

.dp__input_icon {
  left: 0.3rem;
  top: 50%;
  transform: translateY(-50%);
  color: #6b7280;
}

.dp__clear_icon {
  right: 0.875rem;
  top: 50%;
  transform: translateY(-50%);
  color: #6b7280;
}
</style>
