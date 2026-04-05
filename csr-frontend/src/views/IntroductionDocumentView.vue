<template>
  <div class="document-container">
    <!-- Local Header -->
    <header class="sticky top-0 bg-emerald-500 text-white p-4 shadow-md z-10 font-bold text-center flex items-center justify-between print:hidden">
      <span class="text-xl">📄</span>
      <span>เอกสารแนะนำนักเรียนและครอบครัว</span>
      <button @click="printDocument" class="bg-white text-emerald-600 px-3 py-1 rounded-lg text-sm font-bold hover:bg-emerald-50 transition">
        พิมพ์ / บันทึก PDF
      </button>
    </header>

    <main class="p-4">
      <div v-if="isLoading" class="text-center text-gray-500 py-10 animate-pulse">
        กำลังโหลดข้อมูล...
      </div>

      <div v-else-if="document" class="document-content">
        <!-- Document Title -->
        <div class="text-center mb-6">
          <h1 class="text-2xl font-bold text-gray-800">ใบแนะนำตัวนักเรียนและผู้ปกครอง</h1>
          <p class="text-sm text-gray-500 mt-1">ปีการศึกษา 2568</p>
        </div>

        <!-- Photo Row -->
        <div class="photo-row flex justify-center gap-8 mb-6">
          <div class="photo-box">
            <img v-if="studentPhotoUrl" :src="studentPhotoUrl" alt="รูปนักเรียน" class="w-32 h-40 object-cover rounded-lg border-2 border-gray-300" />
            <div v-else class="w-32 h-40 bg-gray-100 rounded-lg border-2 border-dashed border-gray-300 flex items-center justify-center text-gray-400 text-sm">
              ไม่มีรูป
            </div>
            <p class="text-center text-sm mt-2 font-medium">รูปนักเรียน</p>
          </div>
          
          <div v-for="guardian in visibleGuardians" :key="guardian.order" class="photo-box">
            <img v-if="getGuardianPhotoUrl(guardian.order)" :src="getGuardianPhotoUrl(guardian.order)" :alt="`รูปผู้ปกครองคนที่ ${guardian.order}`" class="w-32 h-40 object-cover rounded-lg border-2 border-gray-300" />
            <div v-else class="w-32 h-40 bg-gray-100 rounded-lg border-2 border-dashed border-gray-300 flex items-center justify-center text-gray-400 text-sm">
              ไม่มีรูป
            </div>
            <p class="text-center text-sm mt-2 font-medium">รูปผู้ปกครอง {{ guardian.order }}</p>
          </div>
        </div>

        <!-- Student Section -->
        <section class="section-box bg-gray-50 p-4 rounded-xl border border-gray-200 mb-6">
          <h2 class="font-bold text-gray-800 mb-3 border-b pb-2 text-lg">👤 ข้อมูลนักเรียน</h2>
          <div class="grid grid-cols-2 gap-4">
            <div>
              <span class="text-gray-500 text-sm">รหัสประจำตัว:</span>
              <p class="font-medium">{{ document.student.studentId || '-' }}</p>
            </div>
            <div>
              <span class="text-gray-500 text-sm">ชื่อ-นามสกุล:</span>
              <p class="font-medium">{{ fullName(document.student) }}</p>
            </div>
            <div>
              <span class="text-gray-500 text-sm">ชื่อเล่น:</span>
              <p class="font-medium">{{ document.student.nickname || '-' }}</p>
            </div>
            <div>
              <span class="text-gray-500 text-sm">ห้อง/เลขที่:</span>
              <p class="font-medium">{{ document.student.room }} {{ document.student.newNo ? `เลขที่ ${document.student.newNo}` : '' }}</p>
            </div>
            <div>
              <span class="text-gray-500 text-sm">เบอร์โทรศัพท์:</span>
              <p class="font-medium">{{ document.student.phone || '-' }}</p>
            </div>
            <div>
              <span class="text-gray-500 text-sm">กรุ๊ปเลือด:</span>
              <p class="font-medium">{{ document.student.bloodType || '-' }}</p>
            </div>
            <div>
              <span class="text-gray-500 text-sm">วันเกิด:</span>
              <p class="font-medium">{{ formatDate(document.student.dob) }}</p>
            </div>
          </div>
        </section>

        <!-- Guardian Sections -->
        <section v-for="guardian in visibleGuardians" :key="guardian.order" class="section-box p-4 rounded-xl border mb-6" :class="guardian.order === 1 ? 'bg-emerald-50 border-emerald-200' : 'bg-teal-50 border-teal-200'">
          <h2 class="font-bold mb-3 border-b pb-2 text-lg" :class="guardian.order === 1 ? 'text-emerald-800 border-emerald-200' : 'text-teal-800 border-teal-200'">
            🛡️ ข้อมูลผู้ปกครองคนที่ {{ guardian.order }} {{ guardian.order === 1 ? '(หลัก)' : '' }}
          </h2>
          <div class="grid grid-cols-2 gap-4">
            <div>
              <span class="text-sm" :class="guardian.order === 1 ? 'text-emerald-600' : 'text-teal-600'">ชื่อ-นามสกุล:</span>
              <p class="font-medium">{{ fullName(guardian) }}</p>
            </div>
            <div>
              <span class="text-sm" :class="guardian.order === 1 ? 'text-emerald-600' : 'text-teal-600'">ความสัมพันธ์:</span>
              <p class="font-medium">{{ relationLabel(guardian.relationType) }}</p>
            </div>
            <div>
              <span class="text-sm" :class="guardian.order === 1 ? 'text-emerald-600' : 'text-teal-600'">เบอร์โทรศัพท์:</span>
              <p class="font-medium">{{ guardian.phone || '-' }}</p>
            </div>
            <div>
              <span class="text-sm" :class="guardian.order === 1 ? 'text-emerald-600' : 'text-teal-600'">อาชีพ:</span>
              <p class="font-medium">{{ guardian.occupation || '-' }}</p>
            </div>
            <div>
              <span class="text-sm" :class="guardian.order === 1 ? 'text-emerald-600' : 'text-teal-600'">อีเมล:</span>
              <p class="font-medium">{{ guardian.email || '-' }}</p>
            </div>
            <div>
              <span class="text-sm" :class="guardian.order === 1 ? 'text-emerald-600' : 'text-teal-600'">LINE ID:</span>
              <p class="font-medium">{{ guardian.lineUserId || '-' }}</p>
            </div>
          </div>
        </section>

        <!-- Signature Section -->
        <section class="signature-section mt-8 pt-6 border-t border-gray-300">
          <div class="flex justify-between">
            <div class="text-center">
              <p class="text-sm text-gray-500 mb-16">ลายเซ็นนักเรียน</p>
              <p class="text-sm">(.........................................)</p>
            </div>
            <div class="text-center">
              <p class="text-sm text-gray-500 mb-16">ลายเซ็นผู้ปกครอง</p>
              <p class="text-sm">(.........................................)</p>
            </div>
          </div>
        </section>
      </div>

      <div v-else class="text-center text-gray-500 py-10">
        ไม่พบข้อมูล กรุณาลงทะเบียนก่อน
      </div>
    </main>
  </div>
</template>

<script setup>
import { computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useLiff } from '../composables/useLiff';
import { useIntroductionDocument } from '../composables/useIntroductionDocument';

const router = useRouter();
const { initLiff, isReady, getAccessToken } = useLiff();
const {
  document,
  isLoading,
  studentPhotoUrl,
  fetchDocument,
  getGuardianPhotoUrl,
  printDocument
} = useIntroductionDocument();

const visibleGuardians = computed(() => {
  if (!document.value?.guardians) return [];
  return document.value.guardians.filter(g => g.firstName || g.lastName);
});

const fullName = (person) => {
  if (!person) return '-';
  const parts = [person.firstName, person.lastName].filter(Boolean);
  return parts.length > 0 ? parts.join(' ') : '-';
};

const relationLabel = (type) => {
  const labels = {
    Father: 'บิดา',
    Mother: 'มารดา',
    Other: 'ผู้ปกครองอื่นๆ'
  };
  return labels[type] || type || '-';
};

const formatDate = (dateStr) => {
  if (!dateStr) return '-';
  try {
    const date = new Date(dateStr);
    return date.toLocaleDateString('th-TH', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  } catch {
    return dateStr;
  }
};

onMounted(async () => {
  await initLiff();
  const token = getAccessToken();
  if (token) {
    try {
      await fetchDocument(token);
    } catch (error) {
      console.error('Failed to fetch document:', error);
      if (error.status === 404) {
        router.push('/register');
      }
    }
  }
});
</script>

<style scoped>
@media print {
  .document-container {
    padding: 0;
    margin: 0;
  }
  
  .print\:hidden {
    display: none !important;
  }
  
  .document-content {
    max-width: 100%;
    padding: 0;
  }
  
  .section-box {
    break-inside: avoid;
    page-break-inside: avoid;
  }
  
  .photo-row {
    break-after: avoid;
    page-break-after: avoid;
  }
  
  .signature-section {
    break-before: avoid;
    page-break-before: avoid;
  }
}

@media print {
  @page {
    size: A4;
    margin: 1.5cm;
  }
  
  body {
    font-size: 12pt;
    line-height: 1.4;
  }
  
  .section-box {
    margin-bottom: 0.5cm;
    padding: 0.3cm;
  }
  
  .photo-box img,
  .photo-box div {
    width: 2.5cm;
    height: 3cm;
  }
}
</style>
