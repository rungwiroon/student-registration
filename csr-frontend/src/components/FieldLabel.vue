<template>
  <label class="block text-sm font-medium" :class="labelClass">
    <slot />
    <span v-if="required" class="text-red-500 ml-0.5">*</span>
  </label>
</template>

<script setup>
defineProps({
  required: {
    type: Boolean,
    default: false
  },
  labelClass: {
    type: String,
    default: 'text-gray-700'
  }
})
</script>
