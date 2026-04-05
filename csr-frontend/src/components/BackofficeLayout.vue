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
      </nav>
      <div class="p-4 bg-slate-900 border-t border-slate-700 text-sm">
        เครือข่ายผู้ปกครอง ม.1/2
      </div>
    </aside>

    <!-- Main Content -->
    <div class="flex-1 flex flex-col h-screen overflow-hidden pb-16 lg:pb-0">
      <!-- Top header for mobile -->
      <header class="lg:hidden bg-slate-800 text-white p-4 shadow-md font-bold flex justify-center items-center z-10 relative">
        <div class="flex items-center space-x-2">
          <span class="text-xl">👨‍🏫</span>
          <span>ระบบจัดการ</span>
        </div>
      </header>

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
      </nav>
    </div>
  </div>
</template>
