<template>
  <div>
    <!-- Local Header just for Register -->
    <header class="sticky top-0 bg-emerald-500 text-white p-4 shadow-md z-10 font-bold text-center flex items-center justify-center space-x-2">
      <span class="text-xl">📋</span>
      <span>{{ pageTitle }}</span>
    </header>

    <main class="p-4 space-y-6">
    <h1 class="text-xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-emerald-500 to-teal-500 text-center">{{ formTitle }}</h1>
    
    <div v-if="!isReady" class="text-center text-gray-500 py-10 animate-pulse">
      กำลังเชื่อมต่อ LINE...
    </div>

    <form v-else @submit.prevent="submitForm" class="space-y-6">
      <!-- Student Info -->
      <section class="bg-gray-50 p-4 rounded-xl shadow-sm border border-gray-100">
        <h2 class="font-bold text-gray-800 mb-3 border-b pb-2">👤 ข้อมูลนักเรียน</h2>
        
        <div class="space-y-3">
          <div>
            <label class="block text-sm font-medium text-gray-700">รหัสประจำตัว (Student ID)</label>
            <input v-model="form.student.studentId" type="text" class="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-emerald-500 focus:ring focus:ring-emerald-200 transition p-2 border" placeholder="เว้นว่างได้ถ้าโรงเรียนยังไม่ออกให้" />
          </div>
          
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-sm font-medium text-gray-700">ชื่อ</label>
              <input v-model="form.student.firstName" type="text" required class="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-emerald-500 focus:ring focus:ring-emerald-200 transition p-2 border" placeholder="เด็กชาย..." />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700">นามสกุล</label>
              <input v-model="form.student.lastName" type="text" required class="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-emerald-500 focus:ring focus:ring-emerald-200 transition p-2 border" placeholder="นามสกุล" />
            </div>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700">ชื่อเล่น</label>
            <input v-model="form.student.nickname" type="text" class="mt-1 block w-full rounded-md border-gray-300 shadow-sm p-2 border" placeholder="ชื่อเล่น" />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700">เลขที่</label>
            <input v-model.number="form.student.newNo" type="number" class="mt-1 block w-full rounded-md border-gray-300 shadow-sm p-2 border" placeholder="เว้นว่างได้ถ้ายังไม่ทราบ" />
          </div>
          
          <div>
            <label class="block text-sm font-medium text-gray-700">เบอร์โทรศัพท์นักเรียน (ถ้ามี)</label>
            <input v-model="form.student.phone" type="tel" class="mt-1 block w-full rounded-md border-gray-300 shadow-sm p-2 border" placeholder="08..." />
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-sm font-medium text-gray-700">กรุ๊ปเลือด</label>
              <select v-model="form.student.bloodType" class="mt-1 block w-full rounded-md border-gray-300 shadow-sm p-2 border h-[42px]">
                <option value="">-- เลือก --</option>
                <option value="A">A</option>
                <option value="B">B</option>
                <option value="O">O</option>
                <option value="AB">AB</option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700">วันเกิด</label>
              <input v-model="form.student.dob" type="date" class="mt-1 block w-full rounded-md border-gray-300 shadow-sm p-2 border h-[42px]" />
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
      <section class="bg-emerald-50 p-4 rounded-xl shadow-sm border border-emerald-100">
        <h2 class="font-bold text-emerald-800 mb-3 border-b border-emerald-200 pb-2">🛡️ ข้อมูลผู้ปกครองคนที่ 1 (คุณ)</h2>
        
        <div class="space-y-3">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-sm font-medium text-emerald-700">ชื่อ</label>
              <input v-model="form.guardians[0].firstName" type="text" required class="mt-1 block w-full rounded-md border-emerald-300 shadow-sm focus:border-emerald-500 focus:ring focus:ring-emerald-200 transition p-2 border" placeholder="นาย..." />
            </div>
            <div>
              <label class="block text-sm font-medium text-emerald-700">นามสกุล</label>
              <input v-model="form.guardians[0].lastName" type="text" required class="mt-1 block w-full rounded-md border-emerald-300 shadow-sm focus:border-emerald-500 focus:ring focus:ring-emerald-200 transition p-2 border" placeholder="นามสกุล" />
            </div>
          </div>
          
          <div>
            <label class="block text-sm font-medium text-emerald-700">ความสัมพันธ์</label>
            <select v-model="form.guardians[0].relationType" required class="mt-1 block w-full rounded-md border-emerald-300 shadow-sm focus:border-emerald-500 focus:ring focus:ring-emerald-200 transition p-2 border">
              <option value="" disabled>-- เลือก --</option>
              <option value="Father">บิดา</option>
              <option value="Mother">มารดา</option>
              <option value="Other">ผู้ปกครองอื่นๆ</option>
            </select>
          </div>
          
          <div>
            <label class="block text-sm font-medium text-emerald-700">เบอร์โทรศัพท์ติดต่อ</label>
            <input v-model="form.guardians[0].phone" type="tel" required class="mt-1 block w-full rounded-md border-emerald-300 shadow-sm focus:border-emerald-500 focus:ring focus:ring-emerald-200 transition p-2 border" placeholder="08..." />
          </div>

          <div>
            <label class="block text-sm font-medium text-emerald-700">อาชีพ</label>
            <input v-model="form.guardians[0].occupation" type="text" class="mt-1 block w-full rounded-md border-emerald-300 shadow-sm focus:border-emerald-500 focus:ring focus:ring-emerald-200 transition p-2 border" placeholder="ข้าราชการ, พนักงานบริษัท..." />
          </div>

          <div>
            <label class="block text-sm font-medium text-emerald-700">อีเมล</label>
            <input v-model="form.guardians[0].email" type="email" class="mt-1 block w-full rounded-md border-emerald-300 shadow-sm focus:border-emerald-500 focus:ring focus:ring-emerald-200 transition p-2 border" placeholder="name@example.com" />
          </div>
        </div>
      </section>

      <GuardianPhotoUpload
        :model-value="photos.guardianPhoto1"
        :existing-photo-url="existingPhotoUrls.guardian1"
        :access-token="accessToken"
        @update:model-value="(file) => setGuardianPhoto(file, 1)"
      />

      <!-- Guardian 2 Info (Secondary) -->
      <section class="bg-teal-50 p-4 rounded-xl shadow-sm border border-teal-100">
        <h2 class="font-bold text-teal-800 mb-3 border-b border-teal-200 pb-2">🛡️ ข้อมูลผู้ปกครองคนที่ 2 (ถ้ามี)</h2>
        
        <div class="space-y-3">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-sm font-medium text-teal-700">ชื่อ</label>
              <input v-model="form.guardians[1].firstName" type="text" class="mt-1 block w-full rounded-md border-teal-300 shadow-sm focus:border-teal-500 focus:ring focus:ring-teal-200 transition p-2 border" placeholder="นาย..." />
            </div>
            <div>
              <label class="block text-sm font-medium text-teal-700">นามสกุล</label>
              <input v-model="form.guardians[1].lastName" type="text" class="mt-1 block w-full rounded-md border-teal-300 shadow-sm focus:border-teal-500 focus:ring focus:ring-teal-200 transition p-2 border" placeholder="นามสกุล" />
            </div>
          </div>
          
          <div>
            <label class="block text-sm font-medium text-teal-700">ความสัมพันธ์</label>
            <select v-model="form.guardians[1].relationType" class="mt-1 block w-full rounded-md border-teal-300 shadow-sm focus:border-teal-500 focus:ring focus:ring-teal-200 transition p-2 border">
              <option value="">-- เลือก (ถ้ามี) --</option>
              <option value="Father">บิดา</option>
              <option value="Mother">มารดา</option>
              <option value="Other">ผู้ปกครองอื่นๆ</option>
            </select>
          </div>
          
          <div>
            <label class="block text-sm font-medium text-teal-700">เบอร์โทรศัพท์ติดต่อ</label>
            <input v-model="form.guardians[1].phone" type="tel" class="mt-1 block w-full rounded-md border-teal-300 shadow-sm focus:border-teal-500 focus:ring focus:ring-teal-200 transition p-2 border" placeholder="08..." />
          </div>

          <div>
            <label class="block text-sm font-medium text-teal-700">อาชีพ</label>
            <input v-model="form.guardians[1].occupation" type="text" class="mt-1 block w-full rounded-md border-teal-300 shadow-sm focus:border-teal-500 focus:ring focus:ring-teal-200 transition p-2 border" placeholder="ข้าราชการ, พนักงานบริษัท..." />
          </div>

          <div>
            <label class="block text-sm font-medium text-teal-700">อีเมล</label>
            <input v-model="form.guardians[1].email" type="email" class="mt-1 block w-full rounded-md border-teal-300 shadow-sm focus:border-teal-500 focus:ring focus:ring-teal-200 transition p-2 border" placeholder="name@example.com" />
          </div>
        </div>
      </section>

      <GuardianPhotoUpload
        :model-value="photos.guardianPhoto2"
        :existing-photo-url="existingPhotoUrls.guardian2"
        :access-token="accessToken"
        @update:model-value="(file) => setGuardianPhoto(file, 2)"
      />

      <div v-if="isProfileLoading" class="rounded-xl border border-blue-100 bg-blue-50 px-4 py-3 text-sm text-blue-700">
        กำลังโหลดข้อมูลเดิมและรูปที่มีสิทธิ์เข้าถึง...
      </div>

      <button type="submit" :disabled="isSubmitting" class="w-full bg-gradient-to-r from-emerald-500 to-teal-600 text-white font-bold py-3 px-4 rounded-xl shadow-lg hover:shadow-xl disabled:opacity-50 transition-all">
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

const router = useRouter();
const route = useRoute();
const { initLiff, isReady, getAccessToken } = useLiff();
const {
  form,
  photos,
  existingPhotoUrls,
  isSubmitting,
  isProfileLoading,
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
