<template>
  <div>
    <header class="sticky top-0 bg-header-bg text-white p-4 shadow-md z-10 font-bold text-center flex items-center justify-center space-x-2">
      <span class="text-xl">🏫</span>
      <span>ทะเบียนนักเรียน ม.1/2</span>
    </header>
    
    <main class="p-4 pb-20">
      <router-view></router-view>
    </main>

    <!-- Debug Panel -->
    <div v-if="showDebug" class="fixed bottom-24 left-2 right-2 z-50 max-h-40 bg-black/90 text-green-400 text-[10px] font-mono p-2 rounded overflow-auto shadow-lg border border-green-500/30">
      <div class="flex justify-between items-center mb-1 border-b border-green-500/30 pb-1 sticky top-0 bg-black/90">
        <div>
          <span class="font-bold">DEBUG ({{ logs.length }})</span>
          <span class="ml-2 text-gray-500">v{{ version }}</span>
        </div>
        <div class="flex items-center space-x-2">
          <button @click="copyAll" class="text-green-400 text-[10px] border border-green-500/30 rounded px-1">Copy All</button>
          <button @click="clearLogs" class="text-yellow-400 text-[10px] border border-yellow-500/30 rounded px-1">Clear</button>
          <button @click="showDebug = false" class="text-white px-1">×</button>
        </div>
      </div>
      <div v-for="(log, i) in logs" :key="i" :class="logClass(log.type)" class="break-words">
        {{ log.time }} {{ log.msg }}
      </div>
    </div>
    <!-- Bottom Navigation -->
    <nav class="fixed bottom-0 w-full max-w-[480px] bg-surface border-t border-border flex justify-around items-center text-xs font-medium text-text-secondary z-50">
      <router-link to="/dashboard" class="flex flex-col items-center py-3 px-4 flex-1 text-center hover:text-nav-active transition" active-class="text-nav-active font-bold">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" /></svg>
        หน้าแรก
      </router-link>
      <router-link to="/class-list" class="flex flex-col items-center py-3 px-4 flex-1 text-center hover:text-nav-active transition" active-class="text-nav-active font-bold">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" /></svg>
        เพื่อน
      </router-link>
      <router-link to="/contacts" class="flex flex-col items-center py-3 px-4 flex-1 text-center hover:text-nav-active transition" active-class="text-nav-active font-bold">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" /></svg>
        ติดต่อ
      </router-link>
    </nav>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { LIFF_COMPOSABLE_VERSION } from '../composables/useLiff.js';

const STORAGE_KEY = 'liff_debug_logs_v1';
const showDebug = ref(new URLSearchParams(window.location.search).has('debug'));
const logs = ref([]);
const version = ref(LIFF_COMPOSABLE_VERSION);

function logClass(type) {
  if (type === 'error') return 'text-red-400';
  if (type === 'warn') return 'text-yellow-400';
  return 'text-green-400';
}

function copyAll() {
  const text = logs.value.map(l => `${l.time} [${l.type}] ${l.msg}`).join('\n');
  navigator.clipboard.writeText(text).then(() => {
    console.log('[Debug] copied', logs.value.length, 'lines');
  }).catch(err => {
    console.warn('[Debug] copy failed', err);
  });
}

function clearLogs() {
  logs.value = [];
  try {
    sessionStorage.removeItem(STORAGE_KEY);
  } catch { /* ignore */ }
  console.log('[Debug] logs cleared');
}

function saveLogs() {
  try {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(logs.value.slice(-100)));
  } catch { /* ignore */ }
}

function loadLogs() {
  try {
    const stored = sessionStorage.getItem(STORAGE_KEY);
    if (stored) {
      const parsed = JSON.parse(stored);
      if (Array.isArray(parsed)) return parsed;
    }
  } catch { /* ignore */ }
  return [];
}

onMounted(() => {
  // Restore previous logs from before refresh
  const prev = loadLogs();
  if (prev.length > 0) {
    logs.value.push({ type: 'warn', msg: '--- PAGE REFRESHED ---', time: new Date().toLocaleTimeString('th-TH', { hour12: false }) });
    logs.value = logs.value.concat(prev);
  }

  const origLog = console.log;
  const origErr = console.error;
  const origWarn = console.warn;
  const time = () => new Date().toLocaleTimeString('th-TH', { hour12: false });

  function push(type, args) {
    const msg = args.map(a => {
      try {
        if (a instanceof Error) return a.message + ' | ' + (a.stack || '').split('\n').slice(0, 2).join(' | ');
        if (typeof a === 'object') return JSON.stringify(a);
        return String(a);
      } catch { return String(a); }
    }).join(' ');
    logs.value.push({ type, msg, time: time() });
    if (logs.value.length > 100) logs.value.shift();
    saveLogs();
  }

  console.log = (...args) => { push('log', args); origLog(...args); };
  console.error = (...args) => { push('error', args); origErr(...args); };
  console.warn = (...args) => { push('warn', args); origWarn(...args); };

  // Log navigation info
  console.log('[Debug] URL:', window.location.href);
  console.log('[Debug] UA:', navigator.userAgent.slice(0, 60));
});
</script>
