<template>
  <div class="space-y-6">
    <div class="flex justify-between items-center">
      <h1 class="text-2xl font-bold text-gray-800">จัดการเจ้าหน้าที่</h1>
      <button @click="showAddForm = true" class="bg-slate-700 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-slate-800 transition">
        + เพิ่มเจ้าหน้าที่
      </button>
    </div>

    <div v-if="loading" class="text-gray-500 animate-pulse">กำลังโหลดข้อมูล...</div>
    <div v-else-if="error" class="text-red-500 bg-red-50 p-4 rounded-lg">{{ error }}</div>

    <template v-else>
      <!-- Add Staff Form -->
      <div v-if="showAddForm" class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <h2 class="text-lg font-bold text-gray-800 mb-4">เพิ่มเจ้าหน้าที่ใหม่</h2>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">LINE User ID</label>
            <input v-model="newStaff.lineUserId" type="text" class="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-slate-500 outline-none text-sm" placeholder="Uxxxxxxxx..." />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">ชื่อ</label>
            <input v-model="newStaff.name" type="text" class="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-slate-500 outline-none text-sm" placeholder="ชื่อ-นามสกุล" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">บทบาท</label>
            <select v-model="newStaff.role" class="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-slate-500 outline-none text-sm bg-white">
              <option value="Teacher">Teacher</option>
              <option value="ParentNetworkStaff">ParentNetworkStaff</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">เบอร์โทร</label>
            <input v-model="newStaff.phone" type="text" class="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-slate-500 outline-none text-sm" placeholder="08xxxxxxxx" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">ตำแหน่ง</label>
            <input v-model="newStaff.position" type="text" class="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-slate-500 outline-none text-sm" placeholder="ครูที่ปรึกษาคนที่ 1" />
          </div>
          <div class="flex items-center pt-6">
            <label class="flex items-center cursor-pointer">
              <input v-model="newStaff.isVisibleInDirectory" type="checkbox" class="w-4 h-4 text-emerald-600 border-gray-300 rounded focus:ring-emerald-500" />
              <span class="ml-2 text-sm text-gray-700">แสดงในรายชื่อติดต่อ</span>
            </label>
          </div>
        </div>
        <div class="flex items-center space-x-3 mt-4">
          <button @click="handleAdd" class="bg-emerald-500 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-emerald-600 transition" :disabled="saving">
            {{ saving ? 'กำลังบันทึก...' : 'บันทึก' }}
          </button>
          <button @click="showAddForm = false; resetForm()" class="text-gray-500 text-sm underline">ยกเลิก</button>
        </div>
        <p v-if="formError" class="text-red-500 text-sm mt-2">{{ formError }}</p>
      </div>

      <!-- Staff List -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <!-- Mobile Card List -->
        <div class="divide-y divide-gray-100 lg:hidden">
          <div v-for="staff in staffList" :key="'mob-'+staff.id" class="p-4">
            <div class="flex justify-between items-start mb-2">
              <div>
                <p class="font-bold text-slate-800">{{ staff.name }}</p>
                <p class="text-xs text-gray-500 mt-1 font-mono">{{ staff.lineUserId }}</p>
                <p v-if="staff.lineDisplayName" class="text-xs text-slate-500 mt-0.5">LINE: {{ staff.lineDisplayName }}</p>
                <p v-if="staff.phone" class="text-xs text-slate-500 mt-0.5"><span class="font-medium">เบอร์:</span> {{ staff.phone }}</p>
                <p v-if="staff.position" class="text-xs text-slate-500 mt-0.5"><span class="font-medium">ตำแหน่ง:</span> {{ staff.position }}</p>
                <p class="text-xs mt-0.5" :class="staff.isVisibleInDirectory ? 'text-emerald-600' : 'text-gray-400'">{{ staff.isVisibleInDirectory ? '📂 แสดงในรายชื่อติดต่อ' : '🙈 ซ่อนในรายชื่อติดต่อ' }}</p>
              </div>
              <span class="px-2.5 py-1 text-[10px] font-bold rounded-full uppercase tracking-wide"
                    :class="staff.isActive ? (staff.role === 'Teacher' ? 'bg-emerald-100 text-emerald-700' : 'bg-blue-100 text-blue-700') : 'bg-gray-100 text-gray-500'">
                {{ staff.isActive ? staff.role : 'Disabled' }}
              </span>
            </div>
            <div class="flex items-center space-x-2 mt-2" v-if="staff.isActive">
              <button @click="startEdit(staff)" class="text-xs text-slate-600 hover:underline">แก้ไข</button>
              <span class="text-gray-300">|</span>
              <button @click="toggleRole(staff)" class="text-xs text-blue-600 hover:underline">
                เปลี่ยนบทบาท
              </button>
              <span class="text-gray-300">|</span>
              <button @click="toggleDirectory(staff)" class="text-xs" :class="staff.isVisibleInDirectory ? 'text-gray-500 hover:underline' : 'text-emerald-600 hover:underline'">
                {{ staff.isVisibleInDirectory ? 'ซ่อน' : 'แสดง' }}
              </button>
              <span class="text-gray-300">|</span>
              <button @click="handleDelete(staff)" class="text-xs text-red-500 hover:underline">ปิดใช้งาน</button>
            </div>
            <!-- Inline edit (mobile) -->
            <div v-if="editingId === staff.id" class="mt-3 space-y-2">
              <div>
                <label class="text-xs text-gray-500">ชื่อ</label>
                <input v-model="editName" type="text" class="w-full px-2 py-1 border rounded text-sm" @keyup.enter="saveEdit(staff)" @keyup.escape="editingId = null" />
              </div>
              <div>
                <label class="text-xs text-gray-500">เบอร์โทร</label>
                <input v-model="editPhone" type="text" class="w-full px-2 py-1 border rounded text-sm" @keyup.enter="saveEdit(staff)" @keyup.escape="editingId = null" />
              </div>
              <div>
                <label class="text-xs text-gray-500">ตำแหน่ง</label>
                <input v-model="editPosition" type="text" class="w-full px-2 py-1 border rounded text-sm" @keyup.enter="saveEdit(staff)" @keyup.escape="editingId = null" />
              </div>
              <div class="flex items-center space-x-2">
                <button @click="saveEdit(staff)" class="text-xs text-emerald-600 font-medium">บันทึก</button>
                <button @click="editingId = null" class="text-xs text-gray-500">ยกเลิก</button>
              </div>
            </div>
          </div>
        </div>

        <!-- Desktop Table -->
        <div class="overflow-x-auto hidden lg:block">
          <table class="w-full text-left text-sm text-gray-600">
            <thead class="bg-gray-50 text-gray-700 font-medium">
              <tr>
                <th class="px-6 py-4 border-b">ชื่อ</th>
                <th class="px-6 py-4 border-b">LINE User ID</th>
                <th class="px-6 py-4 border-b">ชื่อ LINE</th>
                <th class="px-6 py-4 border-b">เบอร์โทร</th>
                <th class="px-6 py-4 border-b">ตำแหน่ง</th>
                <th class="px-6 py-4 border-b">บทบาท</th>
                <th class="px-6 py-4 border-b">สถานะ</th>
                <th class="px-6 py-4 border-b">แสดงในรายชื่อ</th>
                <th class="px-6 py-4 border-b text-center">จัดการ</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100">
              <tr v-for="staff in staffList" :key="staff.id" class="hover:bg-slate-50 transition" :class="{ 'opacity-50': !staff.isActive }">
                <td class="px-6 py-4 font-bold text-slate-800">
                  <template v-if="editingId === staff.id">
                    <input v-model="editName" type="text" class="px-2 py-1 border rounded text-sm w-full" @keyup.enter="saveEdit(staff)" @keyup.escape="editingId = null" />
                  </template>
                  <template v-else>{{ staff.name }}</template>
                </td>
                <td class="px-6 py-4 text-xs text-gray-400 font-mono">{{ staff.lineUserId }}</td>
                <td class="px-6 py-4 text-sm text-slate-600">{{ staff.lineDisplayName || '-' }}</td>
                <td class="px-6 py-4 text-xs font-mono">
                  <template v-if="editingId === staff.id">
                    <input v-model="editPhone" type="text" class="px-2 py-1 border rounded text-sm w-full font-mono" @keyup.enter="saveEdit(staff)" @keyup.escape="editingId = null" />
                  </template>
                  <template v-else>{{ staff.phone || '-' }}</template>
                </td>
                <td class="px-6 py-4 text-xs text-slate-500">
                  <template v-if="editingId === staff.id">
                    <input v-model="editPosition" type="text" class="px-2 py-1 border rounded text-sm w-full" @keyup.enter="saveEdit(staff)" @keyup.escape="editingId = null" />
                  </template>
                  <template v-else>{{ staff.position || '-' }}</template>
                </td>
                <td class="px-6 py-4">
                  <span class="px-2.5 py-1 text-xs font-bold rounded-full uppercase tracking-wide"
                        :class="staff.role === 'Teacher' ? 'bg-emerald-100 text-emerald-700' : 'bg-blue-100 text-blue-700'">
                    {{ staff.role }}
                  </span>
                </td>
                <td class="px-6 py-4">
                  <span class="text-xs" :class="staff.isActive ? 'text-emerald-600' : 'text-gray-400'">
                    {{ staff.isActive ? 'ใช้งาน' : 'ปิดใช้งาน' }}
                  </span>
                </td>
                <td class="px-6 py-4">
                  <span class="text-xs" :class="staff.isVisibleInDirectory ? 'text-emerald-600' : 'text-gray-400'">
                    {{ staff.isVisibleInDirectory ? 'แสดง' : 'ซ่อน' }}
                  </span>
                </td>
                <td class="px-6 py-4 text-center">
                  <template v-if="staff.isActive">
                    <button v-if="editingId !== staff.id" @click="startEdit(staff)" class="text-slate-600 hover:text-slate-800 text-xs font-medium px-2 py-1 rounded hover:bg-slate-50 transition">
                      แก้ไข
                    </button>
                    <template v-else>
                      <button @click="saveEdit(staff)" class="text-emerald-600 hover:text-emerald-800 text-xs font-medium px-2 py-1 rounded hover:bg-emerald-50 transition">
                        บันทึก
                      </button>
                      <button @click="editingId = null" class="text-gray-500 text-xs font-medium px-2 py-1 rounded hover:bg-gray-50 transition ml-1">
                        ยกเลิก
                      </button>
                    </template>
                    <button @click="toggleRole(staff)" class="text-blue-600 hover:text-blue-800 text-xs font-medium px-2 py-1 rounded hover:bg-blue-50 transition ml-2">
                      เปลี่ยนบทบาท
                    </button>
                    <button @click="toggleDirectory(staff)" class="text-xs font-medium px-2 py-1 rounded transition ml-2" :class="staff.isVisibleInDirectory ? 'text-gray-500 hover:text-gray-700 hover:bg-gray-50' : 'text-emerald-600 hover:text-emerald-800 hover:bg-emerald-50'">
                      {{ staff.isVisibleInDirectory ? 'ซ่อน' : 'แสดง' }}
                    </button>
                    <button @click="handleDelete(staff)" class="text-red-500 hover:text-red-700 text-xs font-medium px-2 py-1 rounded hover:bg-red-50 transition ml-2">
                      ปิดใช้งาน
                    </button>
                  </template>
                  <span v-else class="text-gray-400 text-xs">—</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div v-if="staffList.length === 0" class="p-10 text-center text-gray-400">
          <p>ยังไม่มีข้อมูลเจ้าหน้าที่</p>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { fetchStaffList, createStaff, updateStaff, deleteStaff } from '../../services/backofficeApi';
import { useLiff } from '../../composables/useLiff';
import { useBackofficeAuth } from '../../composables/useBackofficeAuth';
import { useRouter } from 'vue-router';

const { initLiff, getAccessToken } = useLiff();
const { loadCurrentUser, canManageStaff } = useBackofficeAuth();
const router = useRouter();

const loading = ref(true);
const error = ref('');
const staffList = ref([]);
const saving = ref(false);
const formError = ref('');

const showAddForm = ref(false);
const newStaff = ref({ lineUserId: '', name: '', role: 'Teacher', phone: '', position: '', isVisibleInDirectory: true });

const editingId = ref(null);
const editName = ref('');
const editPhone = ref('');
const editPosition = ref('');

const resetForm = () => {
  newStaff.value = { lineUserId: '', name: '', role: 'Teacher', phone: '', position: '', isVisibleInDirectory: true };
  formError.value = '';
};

const formatDate = (dateStr) => {
  if (!dateStr) return '-';
  const d = new Date(dateStr);
  return d.toLocaleDateString('th-TH', { year: 'numeric', month: 'short', day: 'numeric' });
};

const loadStaff = async () => {
  const token = getAccessToken();
  try {
    staffList.value = await fetchStaffList(token);
  } catch (err) {
    if (err.status === 403) {
      error.value = 'ไม่มีสิทธิ์เข้าถึงส่วนนี้';
    } else {
      error.value = 'เกิดข้อผิดพลาดในการโหลดข้อมูล';
    }
  }
};

onMounted(async () => {
  await initLiff();
  const token = getAccessToken();
  if (!token) {
    router.push('/');
    return;
  }

  try {
    await loadCurrentUser(token);
    if (!canManageStaff()) {
      error.value = 'ไม่มีสิทธิ์เข้าถึงส่วนนี้';
      loading.value = false;
      return;
    }
    await loadStaff();
  } catch (err) {
    error.value = 'เกิดข้อผิดพลาดในการโหลดข้อมูล';
  } finally {
    loading.value = false;
  }
});

const handleAdd = async () => {
  formError.value = '';
  if (!newStaff.value.lineUserId || !newStaff.value.name) {
    formError.value = 'กรุณากรอกข้อมูลให้ครบ';
    return;
  }
  saving.value = true;
  try {
    const token = getAccessToken();
    await createStaff(newStaff.value, token);
    showAddForm.value = false;
    resetForm();
    await loadStaff();
  } catch (err) {
    formError.value = err.message || 'เกิดข้อผิดพลาด';
  } finally {
    saving.value = false;
  }
};

const toggleRole = async (staff) => {
  const newRole = staff.role === 'Teacher' ? 'ParentNetworkStaff' : 'Teacher';
  if (!confirm(`เปลี่ยนบทบาท ${staff.name} เป็น ${newRole}?`)) return;
  try {
    const token = getAccessToken();
    await updateStaff(staff.id, { role: newRole }, token);
    await loadStaff();
  } catch (err) {
    alert('เปลี่ยนบทบาทไม่สำเร็จ: ' + (err.message || ''));
  }
};

const toggleDirectory = async (staff) => {
  const next = !staff.isVisibleInDirectory;
  const action = next ? 'แสดง' : 'ซ่อน';
  if (!confirm(`${action} ${staff.name} ในรายชื่อติดต่อ?`)) return;
  try {
    const token = getAccessToken();
    await updateStaff(staff.id, { isVisibleInDirectory: next }, token);
    await loadStaff();
  } catch (err) {
    alert('เปลี่ยนสถานะไม่สำเร็จ: ' + (err.message || ''));
  }
};

const startEdit = (staff) => {
  editingId.value = staff.id;
  editName.value = staff.name;
  editPhone.value = staff.phone || '';
  editPosition.value = staff.position || '';
};

const saveEdit = async (staff) => {
  if (!editName.value.trim()) return;
  try {
    const token = getAccessToken();
    await updateStaff(staff.id, { name: editName.value.trim(), phone: editPhone.value, position: editPosition.value }, token);
    editingId.value = null;
    await loadStaff();
  } catch (err) {
    alert('แก้ไขไม่สำเร็จ: ' + (err.message || ''));
  }
};

const handleDelete = async (staff) => {
  if (!confirm(`ปิดใช้งาน ${staff.name}?`)) return;
  try {
    const token = getAccessToken();
    await deleteStaff(staff.id, token);
    await loadStaff();
  } catch (err) {
    alert('ปิดใช้งานไม่สำเร็จ: ' + (err.message || ''));
  }
};
</script>
