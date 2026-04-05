<template>
  <div>
    <!-- Local Header just for Register -->
    <header class="sticky top-0 bg-emerald-500 text-white p-4 shadow-md z-10 font-bold text-center flex items-center justify-center space-x-2">
      <span class="text-xl">📋</span>
      <span>ลงทะเบียนข้อมูล</span>
    </header>

    <main class="p-4 space-y-6">
    <h1 class="text-xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-emerald-500 to-teal-500 text-center">ลงทะเบียนผู้ปกครองและนักเรียน</h1>
    
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
          
          <div>
            <label class="block text-sm font-medium text-gray-700">ชื่อ-นามสกุล (จริง)</label>
            <input v-model="form.student.name" type="text" required class="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-emerald-500 focus:ring focus:ring-emerald-200 transition p-2 border" placeholder="เด็กชาย..." />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700">เลขที่ (ใหม่)</label>
            <input v-model.number="form.student.newNo" type="number" class="mt-1 block w-full rounded-md border-gray-300 shadow-sm p-2 border" placeholder="เว้นว่างได้ถ้ายังไม่ทราบ" />
          </div>
          
          <div>
            <label class="block text-sm font-medium text-gray-700">เบอร์โทรศัพท์นักเรียน (ถ้ามี)</label>
            <input v-model="form.student.phone" type="tel" class="mt-1 block w-full rounded-md border-gray-300 shadow-sm p-2 border" placeholder="08..." />
          </div>
        </div>
      </section>

      <!-- Guardian Info -->
      <section class="bg-emerald-50 p-4 rounded-xl shadow-sm border border-emerald-100">
        <h2 class="font-bold text-emerald-800 mb-3 border-b border-emerald-200 pb-2">🛡️ ข้อมูลผู้ปกครอง (คุณ)</h2>
        
        <div class="space-y-3">
          <div>
            <label class="block text-sm font-medium text-emerald-700">ชื่อ-นามสกุล (ผู้ปกครอง)</label>
            <input v-model="form.guardian.name" type="text" required class="mt-1 block w-full rounded-md border-emerald-300 shadow-sm focus:border-emerald-500 focus:ring focus:ring-emerald-200 transition p-2 border" placeholder="นาย..." />
          </div>
          
          <div>
            <label class="block text-sm font-medium text-emerald-700">ความสัมพันธ์</label>
            <select v-model="form.guardian.relationType" required class="mt-1 block w-full rounded-md border-emerald-300 shadow-sm focus:border-emerald-500 focus:ring focus:ring-emerald-200 transition p-2 border">
              <option value="" disabled>-- เลือก --</option>
              <option value="Father">บิดา</option>
              <option value="Mother">มารดา</option>
              <option value="Other">ผู้ปกครองอื่นๆ</option>
            </select>
          </div>
          
          <div>
            <label class="block text-sm font-medium text-emerald-700">เบอร์โทรศัพท์ติดต่อ</label>
            <input v-model="form.guardian.phone" type="tel" required class="mt-1 block w-full rounded-md border-emerald-300 shadow-sm focus:border-emerald-500 focus:ring focus:ring-emerald-200 transition p-2 border" placeholder="08..." />
          </div>
        </div>
      </section>

      <button type="submit" :disabled="isLoading" class="w-full bg-gradient-to-r from-emerald-500 to-teal-600 text-white font-bold py-3 px-4 rounded-xl shadow-lg hover:shadow-xl disabled:opacity-50 transition-all">
        <span v-if="isLoading">พริบตาเดียว... กำลังบันทึกข้อมูล 🚀</span>
        <span v-else>ลงทะเบียนข้อมูลทันที</span>
      </button>
    </form>
    </main>
  </div>
</template>

<script setup>
import { reactive, ref, onMounted } from 'vue';
import { useLiff } from '../composables/useLiff';

const { initLiff, isReady, profile } = useLiff();

const form = reactive({
  student: {
    studentId: '',
    name: '',
    oldRoom: '',
    oldNo: null,
    newRoom: 'ม.1/2',
    newNo: null,
    phone: '',
    bloodType: '',
    dob: ''
  },
  guardian: {
    relationType: '',
    name: '',
    phone: '',
    occupation: '',
    email: ''
  }
});

const isLoading = ref(false);

onMounted(() => {
  initLiff();
});

const submitForm = async () => {
  if (isLoading.value) return;
  isLoading.value = true;

  try {
    const payload = {
      student: {
        studentId: form.student.studentId || null,
        name: form.student.name,
        oldRoom: form.student.oldRoom || null,
        oldNo: form.student.oldNo || null,
        newRoom: form.student.newRoom || null,
        newNo: form.student.newNo || null,
        phone: form.student.phone || '',
        bloodType: form.student.bloodType || '',
        dob: form.student.dob || ''
      },
      guardian: {
        relationType: form.guardian.relationType,
        name: form.guardian.name,
        phone: form.guardian.phone,
        occupation: form.guardian.occupation || '',
        email: form.guardian.email || ''
      }
    };

    // จำลอง Token ตอนพัฒนา หากใช้ liff.getAccessToken() ของจริงจะดีกว่าหากรันบนพาเนล LINE จริง
    const token = profile.value?.userId === 'mock-line-uid-1234' ? 'mock-token' : 'mock-token'; // Fallback to mock token for dev demo

    const response = await fetch('/api/register', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(payload)
    });

    if (response.ok) {
      alert('🎉 ลงทะเบียนสำเร็จเรียบร้อย ข้อมูลเข้าสู่ระบบแล้วครับ!');
      // รีเซ็ตฟอร์ม หรือเด้งไปหน้าถัดไป...
    } else {
      const errorText = await response.text();
      alert(`⚠️ เกิดข้อผิดพลาด: ${errorText}`);
    }
  } catch (error) {
    console.error('API Error:', error);
    alert('❌ ไม่สามารถติดต่อเซิร์ฟเวอร์ได้ ตรวจสอบว่า CsrApi ถูกรันอยู่หรือไม่รัน');
  } finally {
    isLoading.value = false;
  }
};
</script>
