<template>
  <PhotoUploadField
    :model-value="modelValue"
    :existing-photo-url="existingPhotoUrl"
    :access-token="accessToken"
    title="รูปผู้ปกครอง"
    description="อัปโหลดรูปผู้ปกครองเพื่อผูกกับบัญชี LINE และเรียกดูผ่าน API ที่ต้องยืนยันตัวตน"
    theme="teal"
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
