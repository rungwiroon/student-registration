<template>
  <div class="min-h-screen flex bg-gray-50">
    <!-- Sidebar -->
    <aside class="w-64 bg-slate-800 text-white hidden lg:flex flex-col">
      <div class="p-4 bg-slate-900 shadow font-bold text-xl flex items-center space-x-2">
        <span>👨‍🏫</span>
        <span>ระบบจัดการ</span>
      </div>
      <nav class="flex-1 p-4 space-y-2">
        <router-link to="/backoffice/dashboard" class="block py-2 px-4 rounded hover:bg-slate-700" active-class="bg-slate-700 font-bold">
          แดชบอร์ด
        </router-link>
        <router-link to="/backoffice/students" class="block py-2 px-4 rounded hover:bg-slate-700" active-class="bg-slate-700 font-bold">
          รายชื่อนักเรียน
        </router-link>
        <router-link v-if="canManageStaff()" to="/backoffice/staff" class="block py-2 px-4 rounded hover:bg-slate-700" active-class="bg-slate-700 font-bold">
          จัดการเจ้าหน้าที่
        </router-link>
      </nav>
      <!-- Role badge in sidebar -->
      <div class="p-4 bg-slate-900 border-t border-slate-700">
        <div class="text-sm font-medium" :class="isTeacher() ? 'text-emerald-400' : 'text-amber-400'">
          {{ currentUser?.name || 'กำลังโหลด...' }}
        </div>
        <div class="text-xs mt-1" :class="isTeacher() ? 'text-emerald-300' : 'text-amber-300'">
          <span v-if="isTeacher()">Teacher — สิทธิ์เต็ม</span>
          <span v-else-if="isParentNetworkStaff()">Read-only — ข้อมูลบางส่วนถูกจำกัด</span>
        </div>
      </div>
    </aside>

    <!-- Main Content -->
    <div class="flex-1 flex flex-col h-screen overflow-hidden pb-16 lg:pb-0">
      <!-- Top header for mobile -->
      <header class="lg:hidden bg-slate-800 text-white p-4 shadow-md font-bold flex justify-between items-center z-10 relative">
        <div class="flex items-center space-x-2">
          <span class="text-xl">👨‍🏫</span>
          <span>ระบบจัดการ</span>
        </div>
        <div v-if="currentUser" class="text-xs px-2 py-1 rounded-full" :class="isTeacher() ? 'bg-emerald-600 text-emerald-100' : 'bg-amber-600 text-amber-100'">
          {{ isTeacher() ? 'Teacher' : 'Read-only' }}
        </div>
      </header>

      <!-- Limited access notice -->
      <div v-if="isParentNetworkStaff()" class="bg-amber-50 border-b border-amber-200 px-4 py-2 text-center text-xs text-amber-700">
        ข้อมูลบางส่วนถูกปิดบังตามสิทธิ์การเข้าถึงและข้อกำหนด PDPA
      </div>

      <main class="flex-1 overflow-y-auto p-4 lg:p-6 bg-slate-50">
        <router-view></router-view>
      </main>

      <!-- Bottom Navigation for mobile -->
      <nav class="lg:hidden fixed bottom-0 left-0 right-0 w-full bg-white border-t border-slate-200 flex justify-around items-center text-xs font-medium text-slate-500 z-50 shadow-[0_-2px_10px_rgba(0,0,0,0.05)] pb-safe">
        <router-link to="/backoffice/dashboard" class="flex flex-col items-center py-2 px-4 flex-1 text-center hover:text-slate-800 transition" active-class="text-slate-800 font-bold bg-slate-50">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
          </svg>
          แดชบอร์ด
        </router-link>
        <router-link to="/backoffice/students" class="flex flex-col items-center py-2 px-4 flex-1 text-center hover:text-slate-800 transition" active-class="text-slate-800 font-bold bg-slate-50">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
          </svg>
          นักเรียน
        </router-link>
        <router-link v-if="canManageStaff()" to="/backoffice/staff" class="flex flex-col items-center py-2 px-4 flex-1 text-center hover:text-slate-800 transition" active-class="text-slate-800 font-bold bg-slate-50">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.066 2.573c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.573 1.066c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.066-2.573c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
          </svg>
          เจ้าหน้าที่
        </router-link>
      </nav>
    </div>
  </div>
</template>

<script setup>
import { onMounted } from 'vue';
import { useBackofficeAuth } from '../composables/useBackofficeAuth';
import { useLiff } from '../composables/useLiff';

const { currentUser, loadCurrentUser, isTeacher, isParentNetworkStaff, canManageStaff } = useBackofficeAuth();
const { initLiff, getAccessToken } = useLiff();

onMounted(async () => {
  await initLiff();
  const token = getAccessToken();
  if (token) {
    await loadCurrentUser(token);
  }
});
</script>
