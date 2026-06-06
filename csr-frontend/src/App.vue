<template>
  <div :class="isBackoffice ? 'min-h-screen bg-surface-muted' : 'max-w-[480px] mx-auto min-h-screen bg-surface-muted shadow-2xl relative overflow-x-hidden'">
    <router-view></router-view>
  </div>
</template>

<script setup>
import { computed, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { useLiff } from './composables/useLiff';

const route = useRoute();
const isBackoffice = computed(() => {
  return route.path.startsWith('/backoffice');
});

const { init } = useLiff();

onMounted(() => {
  // Init LIFF once when the SPA first loads to refresh token & profile
  init();
});
</script>
