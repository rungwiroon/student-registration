<template>
  <PhotoUploadField
    :model-value="modelValue"
    :existing-photo-url="existingPhotoUrl"
    :access-token="accessToken"
    title="รูปนักเรียน"
    description="อัปโหลดรูปนักเรียนเพื่อใช้ยืนยันตัวตนในระบบเฉพาะผู้มีสิทธิ์"
    theme="primary"
    @update:model-value="emit('update:modelValue', $event)"
  />
</template>

<script setup>
import PhotoUploadField from './PhotoUploadField.vue';

defineProps({
  modelValue: {
    type: File,
    default: null
  },
  existingPhotoUrl: {
    type: String,
    default: null
  },
  accessToken: {
    type: String,
    default: ''
  }
});

const emit = defineEmits(['update:modelValue']);
</script>
