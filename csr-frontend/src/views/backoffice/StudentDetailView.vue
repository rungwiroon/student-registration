<template>
  <div class="space-y-6">
    <div class="flex items-center space-x-4">
      <router-link to="/backoffice/students" class="text-gray-500 hover:text-gray-800">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
        </svg>
      </router-link>
      <h1 class="text-2xl font-bold text-gray-800">ข้อมูลนักเรียนรายบุคคล</h1>
    </div>

    <div v-if="loading" class="text-gray-500 animate-pulse">กำลังโหลดข้อมูล...</div>
    <div v-else-if="error" class="text-red-500 bg-red-50 p-4 rounded-lg">{{ error }}</div>

    <div v-else-if="detail">
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <!-- Student Info -->
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 relative">
          <div class="flex justify-between items-start border-b pb-2 mb-4">
            <h2 class="text-lg font-bold text-gray-800">ข้อมูลนักเรียน</h2>
            <div class="w-16 h-20 bg-gray-100 border border-gray-300 rounded overflow-hidden shadow-sm shrink-0">
              <img v-if="studentPhotoUrl" :src="studentPhotoUrl" class="w-full h-full object-cover" />
              <div v-else-if="!canViewPhotos() && detail.student.photoFileName === null" class="w-full h-full flex items-center justify-center text-xs text-gray-400">รูป</div>
              <div v-else-if="!canViewPhotos()" class="w-full h-full flex items-center justify-center text-xs text-amber-500 text-center px-1">ไม่มีสิทธิ์ดู</div>
              <div v-else class="w-full h-full flex items-center justify-center text-xs text-gray-400">รูป</div>
            </div>
          </div>
          <div class="space-y-3">
            <p><span class="text-gray-500 text-sm">รหัสนักเรียน:</span> <span class="font-medium ml-2">{{ detail.student.studentId }}</span></p>
            <p><span class="text-gray-500 text-sm">ชื่อ-นามสกุล:</span> <span class="font-medium ml-2">{{ detail.student.name }}</span></p>
            <p><span class="text-gray-500 text-sm">ชื่อเล่น:</span> <span class="font-medium ml-2">{{ detail.student.nickname || '-' }}</span></p>
            <p><span class="text-gray-500 text-sm">เบอร์โทรศัพท์:</span> <span class="font-medium ml-2">{{ detail.student.phone || '-' }}</span></p>
            <p><span class="text-gray-500 text-sm">สถานะ:</span>
              <!-- Teacher: editable status -->
              <template v-if="canUpdateReviewStatus()">
                <span v-if="!isEditingStatus" class="px-2 py-1 text-xs rounded-full ml-2 cursor-pointer hover:ring-2 ring-gray-300"
                      :class="detail.student.status === 'Pending' ? 'bg-amber-100 text-amber-800' : 'bg-emerald-100 text-emerald-800'"
                      @click="isEditingStatus = true; newStatus = detail.student.status">
                  {{ detail.student.status }} ✏️
                </span>
                <div v-else class="inline-flex items-center ml-2 space-x-2">
                  <select v-model="newStatus" class="border rounded px-2 py-1 text-sm bg-white focus:ring-2 focus:ring-emerald-500">
                    <option value="Pending">Pending (รอดำเนินการ)</option>
                    <option value="Verified">Verified (ตรวจสอบแล้ว)</option>
                    <option value="Incomplete">Incomplete (ไม่สมบูรณ์)</option>
                  </select>
                  <button @click="saveStatus" class="bg-emerald-500 text-white px-2 py-1 rounded text-xs hover:bg-emerald-600" :disabled="savingStatus">บันทึก</button>
                  <button @click="isEditingStatus = false" class="text-gray-500 text-xs underline">ยกเลิก</button>
                </div>
              </template>
              <!-- ParentNetworkStaff: read-only status -->
              <template v-else>
                <span class="px-2 py-1 text-xs rounded-full ml-2"
                      :class="detail.student.status === 'Pending' ? 'bg-amber-100 text-amber-800' : 'bg-emerald-100 text-emerald-800'">
                  {{ detail.student.status }}
                </span>
              </template>
            </p>
          </div>

          <!-- Internal Note -->
          <div class="mt-6 pt-4 border-t border-gray-100">
            <div class="flex justify-between items-center mb-2">
              <h3 class="text-sm font-bold text-gray-700">บันทึกภายใน (Internal Note)</h3>
              <button v-if="canEditInternalNote() && !isEditingNote" @click="isEditingNote = true; newNote = detail.student.internalNote || ''" class="text-xs text-blue-600 hover:underline">✏️ แก้ไข</button>
            </div>
            <!-- Teacher: editable note -->
            <template v-if="canEditInternalNote()">
              <div v-if="!isEditingNote">
                <p class="text-sm text-gray-600 bg-amber-50 p-3 rounded-lg border border-amber-100 min-h-[50px] whitespace-pre-line">
                  {{ detail.student.internalNote || 'ไม่มีบันทึก' }}
                </p>
              </div>
              <div v-else class="space-y-2">
                <textarea v-model="newNote" rows="3" class="w-full border rounded-lg p-2 text-sm focus:ring-2 focus:ring-emerald-500 outline-none" placeholder="พิมพ์บันทึกข้อความสำหรับเจ้าหน้าที่..."></textarea>
                <div class="flex space-x-2">
                  <button @click="saveNote" class="bg-emerald-500 text-white px-3 py-1.5 rounded text-sm font-medium hover:bg-emerald-600" :disabled="savingNote">บันทึกข้อความ</button>
                  <button @click="isEditingNote = false" class="text-gray-500 text-sm underline px-2 py-1.5">ยกเลิก</button>
                </div>
              </div>
            </template>
            <!-- ParentNetworkStaff: read-only -->
            <template v-else>
              <p class="text-sm text-gray-600 bg-amber-50 p-3 rounded-lg border border-amber-100 min-h-[50px] whitespace-pre-line">
                {{ detail.student.internalNote || 'ไม่มีบันทึก' }}
              </p>
            </template>
          </div>
        </div>

        <!-- Guardian Info -->
        <div class="space-y-6">
          <div v-for="(g, index) in detail.guardians" :key="index" class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
            <div class="flex justify-between items-start border-b pb-2 mb-4">
              <h3 class="font-bold text-gray-700">ผู้ปกครองท่านที่ {{ g.guardianOrder }} ({{ formatRelation(g.relationType) }})</h3>
              <div class="w-16 h-20 bg-gray-100 border border-gray-300 rounded overflow-hidden shadow-sm shrink-0">
                <img v-if="guardianPhotoUrls[g.guardianOrder]" :src="guardianPhotoUrls[g.guardianOrder]" class="w-full h-full object-cover" />
                <div v-else-if="!canViewPhotos() && g.photoFileName === null" class="w-full h-full flex items-center justify-center text-xs text-gray-400">รูป</div>
                <div v-else-if="!canViewPhotos()" class="w-full h-full flex items-center justify-center text-xs text-amber-500 text-center px-1">ไม่มีสิทธิ์ดู</div>
                <div v-else class="w-full h-full flex items-center justify-center text-xs text-gray-400">รูป</div>
              </div>
            </div>
            <div class="mt-2 space-y-2 text-sm pl-4 border-l-2 border-emerald-200">
                <p><span class="text-gray-500">ชื่อ-นามสกุล:</span> <span class="font-medium ml-2">{{ g.name || '-' }}</span></p>
                <p><span class="text-gray-500">เบอร์โทรศัพท์:</span> <span class="font-medium ml-2">{{ g.phone || '-' }}</span></p>
                <p><span class="text-gray-500">อาชีพ:</span> <span class="font-medium ml-2">{{ g.occupation || '-' }}</span></p>
              </div>
            </div>
          <div v-if="detail.guardians.length === 0" class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 text-gray-500">
            ยังไม่มีข้อมูลผู้ปกครอง
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { fetchStudentDetail, updateStudentStatus, updateStudentNote, fetchStudentPhotoBlob, fetchGuardianPhotoBlob } from '../../services/backofficeApi';
import { useLiff } from '../../composables/useLiff';
import { useBackofficeAuth } from '../../composables/useBackofficeAuth';
import { useRouter, useRoute } from 'vue-router';

const { initLiff, getAccessToken } = useLiff();
const { loadCurrentUser, canViewPhotos, canUpdateReviewStatus, canEditInternalNote } = useBackofficeAuth();
const router = useRouter();
const route = useRoute();

const loading = ref(true);
const error = ref('');
const detail = ref(null);
const studentPhotoUrl = ref(null);
const guardianPhotoUrls = ref({});

const isEditingStatus = ref(false);
const newStatus = ref('');
const savingStatus = ref(false);

const isEditingNote = ref(false);
const newNote = ref('');
const savingNote = ref(false);

const formatRelation = (rel) => {
  if (rel === 'Father') return 'บิดา';
  if (rel === 'Mother') return 'มารดา';
  return 'อื่นๆ';
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
    detail.value = await fetchStudentDetail(route.params.id, token);

    // Only fetch photos if user has permission
    if (canViewPhotos()) {
      // Fetch student photo
      if (detail.value?.student?.photoFileName) {
        try {
          const studentBlob = await fetchStudentPhotoBlob(route.params.id, token);
          studentPhotoUrl.value = URL.createObjectURL(studentBlob);
        } catch (e) {
          console.warn('Failed to load student photo', e);
        }
      }

      // Fetch guardian photos
      if (detail.value?.guardians) {
        for (const guardian of detail.value.guardians) {
          if (guardian.photoFileName) {
            try {
              const guardianBlob = await fetchGuardianPhotoBlob(route.params.id, guardian.guardianOrder, token);
              guardianPhotoUrls.value[guardian.guardianOrder] = URL.createObjectURL(guardianBlob);
            } catch (e) {
              console.warn(`Failed to load guardian ${guardian.guardianOrder} photo`, e);
            }
          }
        }
      }
    }
  } catch (err) {
    if (err.status === 403) {
      error.value = 'ไม่มีสิทธิ์เข้าถึงส่วนนี้';
    } else {
      error.value = 'เกิดข้อผิดพลาดในการโหลดข้อมูล';
    }
  } finally {
    loading.value = false;
  }
});

const saveStatus = async () => {
  savingStatus.value = true;
  try {
    const token = getAccessToken();
    await updateStudentStatus(route.params.id, newStatus.value, token);
    detail.value.student.status = newStatus.value;
    isEditingStatus.value = false;
  } catch (err) {
    alert('บันทึกสถานะไม่สำเร็จ: ' + err.message);
  } finally {
    savingStatus.value = false;
  }
};

const saveNote = async () => {
  savingNote.value = true;
  try {
    const token = getAccessToken();
    await updateStudentNote(route.params.id, newNote.value, token);
    detail.value.student.internalNote = newNote.value;
    isEditingNote.value = false;
  } catch (err) {
    alert('บันทึกข้อความไม่สำเร็จ: ' + err.message);
  } finally {
    savingNote.value = false;
  }
};
</script>
