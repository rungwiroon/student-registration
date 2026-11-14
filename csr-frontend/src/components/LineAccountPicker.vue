<template>
  <div class="space-y-2">
    <div class="relative">
      <input
        v-model="query"
        type="text"
        class="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-slate-500 outline-none text-sm"
        placeholder="ค้นหาจากชื่อผู้ปกครอง / ชื่อ LINE / เบอร์โทร"
        aria-label="ค้นหาผู้ใช้ LINE"
        @focus="open = true"
        @keyup.escape="open = false"
      />
      <ul
        v-if="open && matches.length"
        class="absolute z-20 left-0 right-0 mt-1 max-h-64 overflow-auto bg-white border border-gray-200 rounded-lg shadow-lg divide-y divide-gray-100"
        role="listbox"
      >
        <li v-for="user in matches" :key="user.lineUserId" role="option">
          <button
            type="button"
            class="w-full text-left px-3 py-2 hover:bg-slate-50 text-sm"
            @click="pick(user)"
          >
            <span class="font-medium text-slate-800">{{ user.name || '-' }}</span>
            <span class="text-xs text-gray-500"> · {{ relation(user.relationType) }}</span>
            <span v-if="user.lineDisplayName" class="block text-xs text-slate-600">LINE: {{ user.lineDisplayName }}</span>
            <span class="block text-xs text-gray-400">{{ user.phone || '-' }}</span>
          </button>
        </li>
      </ul>
      <p v-if="open && query.trim() && !matches.length" class="text-xs text-gray-400 mt-1">ไม่พบในผู้ปกครองที่ลงทะเบียน</p>
    </div>

    <div>
      <label class="block text-xs text-gray-500 mb-1">LINE User ID</label>
      <input
        :value="modelValue"
        type="text"
        class="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-slate-500 outline-none text-xs font-mono"
        placeholder="Uxxxxxxxx..."
        :disabled="disabled"
        aria-label="LINE User ID"
        @input="$emit('update:modelValue', $event.target.value)"
      />
      <p v-if="selected" class="text-xs text-emerald-600 mt-1">
        ผู้ใช้นี้คือ {{ selected.name || '-' }}<template v-if="selected.lineDisplayName"> (LINE: {{ selected.lineDisplayName }})</template>
      </p>
      <p v-else-if="modelValue" class="text-xs text-amber-600 mt-1">ID นี้ไม่ตรงกับผู้ปกครองที่ลงทะเบียน ตรวจให้แน่ใจว่าถูกคน</p>
    </div>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue';

const props = defineProps({
  modelValue: { type: String, default: '' },
  users: { type: Array, default: () => [] },
  disabled: { type: Boolean, default: false }
});
const emit = defineEmits(['update:modelValue', 'pick']);

const query = ref('');
const open = ref(false);

const relation = (rel) => {
  if (rel === 'Father') return 'บิดา';
  if (rel === 'Mother') return 'มารดา';
  if (rel === 'Other') return 'ผู้ปกครองอื่นๆ';
  return rel || '-';
};

const matches = computed(() => {
  const q = query.value.trim().toLowerCase();
  if (!q) return [];
  return props.users
    .filter((u) => [u.name, u.lineDisplayName, u.phone, u.lineUserId].some((v) => (v || '').toLowerCase().includes(q)))
    .slice(0, 8);
});

const selected = computed(() => props.users.find((u) => u.lineUserId === props.modelValue.trim()) || null);

const pick = (user) => {
  emit('update:modelValue', user.lineUserId);
  emit('pick', user);
  query.value = '';
  open.value = false;
};
</script>
