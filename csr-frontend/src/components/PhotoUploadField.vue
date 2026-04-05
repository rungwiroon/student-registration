<template>
  <section :class="sectionClass">
    <div class="flex items-start justify-between gap-3">
      <div>
        <h3 :class="titleClass">{{ title }}</h3>
        <p class="mt-1 text-sm text-gray-500">{{ description }}</p>
      </div>
      <span class="rounded-full px-3 py-1 text-xs font-semibold" :class="badgeClass">{{ badgeText }}</span>
    </div>

    <div class="mt-4 overflow-hidden rounded-2xl border border-dashed border-gray-300 bg-white">
      <div v-if="previewUrl" class="aspect-square w-full bg-gray-100">
        <img :src="previewUrl" :alt="title" class="h-full w-full object-cover" />
      </div>
      <div v-else class="flex aspect-square w-full items-center justify-center bg-gray-50 text-sm text-gray-400">
        ยังไม่มีรูป
      </div>
    </div>

    <div class="mt-4 flex flex-wrap gap-3">
      <label :class="buttonClass">
        <input type="file" accept="image/jpeg,image/png,image/webp" class="hidden" @change="onFileChange" />
        <span>{{ buttonText }}</span>
      </label>
      <button v-if="modelValue" type="button" class="rounded-xl border border-gray-300 px-4 py-2 text-sm font-medium text-gray-600 transition hover:bg-gray-50" @click="clearSelectedFile">
        ล้างรูปที่เลือก
      </button>
    </div>

    <p class="mt-3 text-xs text-gray-500">รองรับไฟล์ JPEG, PNG, WEBP และระบบจะส่งผ่าน API แบบมีสิทธิ์เข้าถึงเท่านั้น</p>
    <p v-if="selectedFileName" class="mt-2 text-sm text-gray-600">ไฟล์ที่เลือก: {{ selectedFileName }}</p>
    <p v-if="loadError" class="mt-2 text-sm text-red-500">{{ loadError }}</p>
  </section>
</template>

<script setup>
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { fetchProtectedPhotoUrl } from '../services/registrationApi';

const props = defineProps({
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
  },
  title: {
    type: String,
    required: true
  },
  description: {
    type: String,
    required: true
  },
  theme: {
    type: String,
    default: 'emerald'
  }
});

const emit = defineEmits(['update:modelValue']);

const localPreviewUrl = ref(null);
const remotePreviewUrl = ref(null);
const loadError = ref('');

const previewUrl = computed(() => localPreviewUrl.value || remotePreviewUrl.value);
const selectedFileName = computed(() => props.modelValue?.name || '');
const hasExistingPhoto = computed(() => Boolean(props.existingPhotoUrl));
const badgeText = computed(() => (props.modelValue ? 'อัปเดตรูปใหม่' : hasExistingPhoto.value ? 'มีรูปเดิม' : 'ยังไม่มีรูป'));
const buttonText = computed(() => (props.modelValue || hasExistingPhoto.value ? 'เลือกรูปใหม่' : 'เลือกรูป'));
const sectionClass = computed(() => props.theme === 'emerald'
  ? 'rounded-2xl border border-gray-100 bg-gray-50 p-4 shadow-sm'
  : 'rounded-2xl border border-emerald-100 bg-emerald-50 p-4 shadow-sm');
const titleClass = computed(() => props.theme === 'emerald' ? 'font-bold text-gray-800' : 'font-bold text-emerald-800');
const badgeClass = computed(() => props.theme === 'emerald'
  ? 'bg-gray-100 text-gray-600'
  : 'bg-emerald-100 text-emerald-700');
const buttonClass = computed(() => props.theme === 'emerald'
  ? 'inline-flex cursor-pointer items-center rounded-xl bg-emerald-500 px-4 py-2 text-sm font-medium text-white transition hover:bg-emerald-600'
  : 'inline-flex cursor-pointer items-center rounded-xl bg-teal-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-teal-700');

watch(
  () => props.modelValue,
  (file) => {
    if (!file) {
      revokeLocalPreview();
      return;
    }

    revokeLocalPreview();
    localPreviewUrl.value = URL.createObjectURL(file);
  },
  { immediate: true }
);

watch(
  () => [props.existingPhotoUrl, props.accessToken, props.modelValue],
  async ([photoUrl, accessToken, selectedFile]) => {
    loadError.value = '';
    revokeRemotePreview();

    if (!photoUrl || !accessToken || selectedFile) {
      return;
    }

    try {
      remotePreviewUrl.value = await fetchProtectedPhotoUrl(accessToken, photoUrl);
    } catch (error) {
      loadError.value = error.message || 'ไม่สามารถโหลดรูปเดิมได้';
    }
  },
  { immediate: true }
);

onBeforeUnmount(() => {
  revokeLocalPreview();
  revokeRemotePreview();
});

function onFileChange(event) {
  const [file] = event.target.files || [];
  emit('update:modelValue', file ?? null);
  event.target.value = '';
}

function clearSelectedFile() {
  emit('update:modelValue', null);
}

function revokeLocalPreview() {
  if (!localPreviewUrl.value) {
    return;
  }

  URL.revokeObjectURL(localPreviewUrl.value);
  localPreviewUrl.value = null;
}

function revokeRemotePreview() {
  if (!remotePreviewUrl.value) {
    return;
  }

  URL.revokeObjectURL(remotePreviewUrl.value);
  remotePreviewUrl.value = null;
}
</script>
