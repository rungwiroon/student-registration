<template>
  <div class="space-y-6 mt-2">
    <div>
      <h1 class="text-2xl font-bold text-text-primary">ติดต่อบุคลากร</h1>
      <p class="text-sm text-text-secondary">ครูที่ปรึกษาและกรรมการห้อง ม.1/2</p>
    </div>

    <!-- Teacher -->
    <section v-if="isLoading || teachers.length > 0">
      <h2 class="mb-3 flex items-center font-bold text-brand-primary-strong"><span class="mr-2 text-xl">👨‍🏫</span> ครูที่ปรึกษา</h2>
      <div class="overflow-hidden rounded-xl border border-border bg-surface shadow-sm divide-y divide-border">
        <div v-if="isLoading" class="p-4 text-sm text-text-secondary animate-pulse">กำลังโหลด...</div>
        <div v-else v-for="teacher in teachers" :key="teacher.id" class="p-4 flex items-center justify-between">
          <div>
            <h3 class="font-bold text-text-primary">{{ teacher.name }}</h3>
            <p v-if="teacher.position" class="text-xs text-text-secondary">{{ teacher.position }}</p>
          </div>
          <a v-if="teacher.phone" :href="`tel:${teacher.phone}`" class="rounded-full bg-brand-primary-soft p-3 text-brand-primary-strong transition hover:bg-brand-primary-soft/80 active:scale-95">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path d="M2 3a1 1 0 011-1h2.153a1 1 0 01.986.836l.74 4.435a1 1 0 01-.54 1.06l-1.548.773a11.037 11.037 0 006.105 6.105l.774-1.548a1 1 0 011.059-.54l4.435.74a1 1 0 01.836.986V17a1 1 0 01-1 1h-2C7.82 18 2 12.18 2 5V3z" />
            </svg>
          </a>
        </div>
      </div>
    </section>

    <!-- Committee -->
    <section>
      <h2 class="mb-3 flex items-center font-bold text-brand-secondary-strong"><span class="mr-2 text-xl">👥</span> เครือข่ายผู้ปกครอง</h2>
      <div class="overflow-hidden rounded-xl border border-border bg-surface shadow-sm divide-y divide-border">
        <div v-if="isLoading" class="p-4 text-sm text-text-secondary animate-pulse">กำลังโหลด...</div>
        <div v-else-if="parentNetwork.length === 0" class="p-4 text-sm text-text-secondary">ยังไม่มีข้อมูล</div>
        <div v-else v-for="person in parentNetwork" :key="person.id" class="p-4 flex items-center justify-between">
          <div>
            <h3 class="font-bold text-text-primary">{{ person.name || '-' }}</h3>
            <p v-if="person.position" class="text-xs text-text-secondary">{{ person.position }}</p>
          </div>
          <a v-if="person.phone" :href="`tel:${person.phone}`" class="rounded-full bg-brand-primary-soft p-3 text-brand-primary-strong transition hover:bg-brand-primary-soft/80 active:scale-95">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path d="M2 3a1 1 0 011-1h2.153a1 1 0 01.986.836l.74 4.435a1 1 0 01-.54 1.06l-1.548.773a11.037 11.037 0 006.105 6.105l.774-1.548a1 1 0 011.059-.54l4.435.74a1 1 0 01.836.986V17a1 1 0 01-1 1h-2C7.82 18 2 12.18 2 5V3z" />
            </svg>
          </a>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useLiff } from '../composables/useLiff';
import { apiJson, UnauthorizedError } from '../services/apiClient';

const { getAccessToken, login } = useLiff();
const isLoading = ref(true);
const teachers = ref([]);
const parentNetwork = ref([]);

onMounted(async () => {
  const token = getAccessToken();
  if (!token) { login(); return; }

  try {
    const data = await apiJson('/api/directory', token);
    teachers.value = data.teachers ?? [];
    parentNetwork.value = data.parentNetwork ?? [];
  } catch (err) {
    if (err instanceof UnauthorizedError) {
      return; // redirectToLogin จัดการแล้ว
    }
    console.warn('Failed to load directory', err);
  } finally {
    isLoading.value = false;
  }
});
</script>
