<template>
  <div class="space-y-4">
    <div class="flex justify-between items-end mb-6 mt-2">
      <div>
        <h1 class="text-2xl font-bold text-gray-800">เพื่อนในห้อง</h1>
        <p class="text-sm text-gray-500">ม.1/2 (สมุดรายชื่อปิดบังPDPA)</p>
      </div>
      <div class="bg-emerald-100 text-emerald-800 text-xs font-bold px-2 py-1 rounded-md">
        {{ mockStudents.length }} คน
      </div>
    </div>

    <!-- List -->
    <div class="bg-white rounded-xl shadow-sm overflow-hidden border border-gray-100 divide-y divide-gray-50">
      <div v-for="(student, index) in mockStudents" :key="index" class="p-4 flex items-center space-x-4 hover:bg-gray-50 transition">
        <div class="w-10 h-10 rounded-xl bg-emerald-50 flex items-center justify-center text-emerald-700 font-bold shrink-0">
          {{ index + 1 }}
        </div>
        <div>
          <h3 class="font-medium text-gray-800">{{ maskFullName(student.name) }}</h3>
          <p class="text-xs text-gray-400">รหัสประจำตัว: <span class="font-mono text-gray-500">{{ student.id || 'รอประกาศ' }}</span></p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { maskFullName } from '../utils/masking';

const mockStudents = [
  { name: 'ด.ช. ก้องเกียรติ สุขเสมอ', id: '31001' },
  { name: 'ด.ช. ขจรศักดิ์ ดีงาม', id: '31002' },
  { name: 'ด.ญ. จิราพร มาดี', id: '31003' },
  { name: 'ด.ช. ชลธี มีโชค', id: '31004' },
  { name: 'ด.ญ. ฐิติมา พรหมวิหาร', id: '' },
  { name: 'ด.ช. ณัฐวุฒิ สมบูรณ์', id: '31006' },
  { name: 'ด.ช. ดนัย แข็งแกร่ง', id: '31007' },
  { name: 'ด.ญ. ตรีเนตร สว่าง', id: '' },
  { name: 'ด.ช. ธนพล มั่นคง', id: '31009' },
  { name: 'ด.ช. พัสรก ใจร้าย', id: '' },
];
</script>
