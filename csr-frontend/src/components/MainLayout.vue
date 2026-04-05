<template>
  <div>
    <header class="sticky top-0 bg-emerald-500 text-white p-4 shadow-md z-10 font-bold text-center flex items-center justify-center space-x-2">
      <span class="text-xl">🏫</span>
      <span>CSR ม.1/2</span>
    </header>
    
    <main class="p-4 pb-20">
      <router-view></router-view>
    </main>

    <!-- Bottom Navigation -->
    <nav class="fixed bottom-0 w-full max-w-[480px] bg-white border-t border-gray-200 flex justify-around items-center text-xs font-medium text-gray-500 z-50">
      <router-link to="/dashboard" class="flex flex-col items-center py-3 px-4 flex-1 text-center hover:text-emerald-600 transition" active-class="text-emerald-600 font-bold">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" /></svg>
        หน้าแรก
      </router-link>
      <router-link to="/class-list" class="flex flex-col items-center py-3 px-4 flex-1 text-center hover:text-emerald-600 transition" active-class="text-emerald-600 font-bold">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" /></svg>
        เพื่อน
      </router-link>
      <router-link to="/contacts" class="flex flex-col items-center py-3 px-4 flex-1 text-center hover:text-emerald-600 transition" active-class="text-emerald-600 font-bold">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" /></svg>
        ติดต่อ
      </router-link>
    </nav>
  </div>
</template>

<script setup>
</script>
