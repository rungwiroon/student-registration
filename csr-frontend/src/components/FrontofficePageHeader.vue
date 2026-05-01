<template>
  <header class="sticky top-0 bg-header-bg text-white p-4 shadow-md z-10 flex items-center justify-between print:hidden">
    <button
      v-if="showBack"
      @click="goBackOrFallback(backFallbackRoute)"
      class="flex items-center gap-1 text-sm font-bold hover:opacity-80 transition shrink-0"
    >
      <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
      </svg>
      กลับ
    </button>
    <span v-else class="w-16"></span>

    <span class="font-bold text-center truncate px-2">{{ title }}</span>

    <button
      v-if="isInClient"
      @click="closeWindow()"
      class="flex items-center gap-1 text-sm font-bold hover:opacity-80 transition shrink-0"
      title="ปิด"
      aria-label="ปิด"
    >
      <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
      </svg>
    </button>
    <slot v-else-if="$slots.actions" name="actions"></slot>
    <button
      v-else-if="showHome"
      @click="goHome()"
      class="flex items-center gap-1 text-sm font-bold hover:opacity-80 transition shrink-0"
    >
      หน้าแรก
      <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
      </svg>
    </button>
    <span v-else class="w-16"></span>
  </header>
</template>

<script setup>
import { useSafeNavigation } from '../composables/useSafeNavigation'
import { useLiff } from '../composables/useLiff'

defineProps({
  title: {
    type: String,
    required: true
  },
  showBack: {
    type: Boolean,
    default: true
  },
  showHome: {
    type: Boolean,
    default: true
  },
  backFallbackRoute: {
    type: String,
    default: '/dashboard'
  }
})

const { goBackOrFallback, goHome } = useSafeNavigation()
const { isInClient, closeWindow } = useLiff()
</script>
