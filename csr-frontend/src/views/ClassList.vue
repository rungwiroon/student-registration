<template>
  <div class="space-y-4">
    <div class="flex justify-between items-end mb-6 mt-2">
      <div>
        <h1 class="text-2xl font-bold text-text-primary">เพื่อนในห้อง</h1>
        <p class="text-sm text-text-secondary">ม.1/2 (สมุดรายชื่อปิดบังPDPA)</p>
      </div>
      <div v-if="!isLoading" class="rounded-md bg-brand-secondary-soft px-2 py-1 text-xs font-bold text-brand-secondary-strong">
        {{ students.length }} คน
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="py-10 text-center text-text-secondary animate-pulse">
      กำลังเรียกดูรายชื่อ...
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="bg-red-50 text-red-500 p-4 rounded-xl text-center">
      {{ error }}
    </div>

    <!-- List -->
    <div v-else class="overflow-hidden rounded-xl border border-border bg-surface shadow-sm divide-y divide-border">
      <div v-for="(student, index) in students" :key="student.id || index" class="flex items-center space-x-4 p-4 transition hover:bg-surface-muted">
        <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-brand-primary-soft font-bold text-brand-primary-strong">
          {{ index + 1 }}
        </div>
        <div>
          <!-- Backend performs PDPA masking -->
          <h3 class="font-medium text-text-primary">{{ student.maskedName }}</h3>
          <p class="text-xs text-text-secondary">รหัสประจำตัว: <span class="font-mono text-text-secondary">{{ student.maskedStudentId || 'รอประกาศ' }}</span></p>
        </div>
      </div>
      
      <div v-if="students.length === 0" class="p-8 text-center text-text-secondary">
        ยังไม่มีรายชื่อเพื่อนในห้องนี้
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useLiff } from '../composables/useLiff';

const students = ref([]);
const isLoading = ref(true);
const error = ref(null);
const { initLiff, getAccessToken } = useLiff();

onMounted(async () => {
  try {
    await initLiff();

    const token = getAccessToken();
    if (!token) {
      error.value = 'ไม่พบ LINE access token สำหรับเรียกใช้งาน API';
      return;
    }

    const response = await fetch('/api/class', {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

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
