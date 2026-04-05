<template>
  <div class="space-y-4">
    <div class="flex justify-between items-end mb-6 mt-2">
      <div>
        <h1 class="text-2xl font-bold text-gray-800">เพื่อนในห้อง</h1>
        <p class="text-sm text-gray-500">ม.1/2 (สมุดรายชื่อปิดบังPDPA)</p>
      </div>
      <div v-if="!isLoading" class="bg-emerald-100 text-emerald-800 text-xs font-bold px-2 py-1 rounded-md">
        {{ students.length }} คน
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="text-center text-gray-500 py-10 animate-pulse">
      กำลังเรียกดูรายชื่อ...
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="bg-red-50 text-red-500 p-4 rounded-xl text-center">
      {{ error }}
    </div>

    <!-- List -->
    <div v-else class="bg-white rounded-xl shadow-sm overflow-hidden border border-gray-100 divide-y divide-gray-50">
      <div v-for="(student, index) in students" :key="student.id || index" class="p-4 flex items-center space-x-4 hover:bg-gray-50 transition">
        <div class="w-10 h-10 rounded-xl bg-emerald-50 flex items-center justify-center text-emerald-700 font-bold shrink-0">
          {{ index + 1 }}
        </div>
        <div>
          <!-- Backend performs PDPA masking -->
          <h3 class="font-medium text-gray-800">{{ student.maskedName }}</h3>
          <p class="text-xs text-gray-400">รหัสประจำตัว: <span class="font-mono text-gray-500">{{ student.maskedStudentId || 'รอประกาศ' }}</span></p>
        </div>
      </div>
      
      <div v-if="students.length === 0" class="p-8 text-center text-gray-400">
        ยังไม่มีรายชื่อเพื่อนในห้องนี้
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';

const students = ref([]);
const isLoading = ref(true);
const error = ref(null);

onMounted(async () => {
  try {
    const response = await fetch('/api/class');
    if (response.ok) {
      students.value = await response.json();
    } else {
      error.value = 'ไม่สามารถดึงข้อมูลรายชื่อได้โปรดลองอีกครั้ง';
    }
  } catch (err) {
    console.error('API Error:', err);
    error.value = 'เกิดข้อผิดพลาดในการเชื่อมต่อเซิร์ฟเวอร์';
  } finally {
    isLoading.value = false;
  }
});
</script>
