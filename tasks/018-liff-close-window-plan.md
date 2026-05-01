# 📱 Phase 18: LIFF Close Window (Android In-App Browser)
**Status:** Completed
**Depends on:** existing `useLiff.js` composable, `FrontofficePageHeader.vue` component
**Goal:** เพิ่มปุ่มปิด/กลับไป LINE app บน Android เมื่อเปิดจาก in-app browser ใน LIFF เนื่องจาก iOS มีปุ่ม close ของ Safari view controller แต่ Android ไม่มี

---

## 🎯 1. Problem Statement

- เปิดเว็บจาก LINE app บน **iOS** → Safari view controller มีปุ่ม `Done` / `Close` อยู่แล้ว
- เปิดเว็บจาก LINE app บน **Android** → Chrome Custom Tab หรือ WebView ไม่มีปุ่มปิด ต้องกดปุ่ม system back เอง
- ผู้ใช้ Android ไม่รู้ว่าต้องกดปุ่ม back ถึงจะกลับไป chat ได้
- ปัญหานี้เกิดเฉพาะเมื่อเปิดเป็น **LIFF app** (ลิงก์ที่แชร์ใน chat เปิดเป็น in-app browser)

**Solution:** ใช้ `liff.isInClient()` + `liff.closeWindow()` เพื่อแสดงปุ่ม X เมื่ออยู่ใน LIFF browser

---

## 🔍 2. Current State

### `useLiff.js`
- Export: `initLiff`, `isReady`, `profile`, `error`, `getAccessToken`
- ไม่มี `isInClient` หรือ `closeWindow`
- **Issue:** `useLiff()` ไม่ใช่ singleton — refs สร้างใหม่ทุกครั้งที่ call. Header กับ page caller จะได้ `isInClient` คนละ instance → ต้อง hoist refs ไป module scope

### `FrontofficePageHeader.vue`
- Props: `title`, `showBack`, `showHome`, `backFallbackRoute`
- มีปุ่ม **กลับ** (ซ้าย) และ **หน้าแรก** (ขวา)
- Slot `#actions` อยู่ใน `v-else` ของ `showHome` (line 27-29). `showHome` default = `true` → slot ไม่ render ถ้าไม่ override prop
- **Latent bug:** `IntroductionDocumentView.vue` ส่ง `#actions` slot (print button) แต่ `showHome` ไม่ได้ override → print button หาย

### Pages ที่ใช้ `FrontofficePageHeader`
- `Register.vue`
- `IntroductionDocumentView.vue` (มี slot `#actions`)

---

## ✅ 3. Task Checklist

### Step 1: Make `useLiff.js` singleton + expose LIFF capabilities
- [x] Hoist `isReady`, `profile`, `accessToken`, `error`, `isInClient` เป็น module-level refs (ไม่ใช่ function-scoped)
- [x] `useLiff()` return refs ที่ชี้ไป module-level state เดียวกัน
- [x] Set `isInClient.value = liff.isInClient()` หลัง `liff.init()` resolve (mock mode → `false`)
- [x] Add `closeWindow()` function → calls `liff.closeWindow()` (mock mode → no-op)
- [x] Return `isInClient` และ `closeWindow` จาก composable
- [x] **Reactivity guarantee:** `isInClient` เป็น `ref` → header re-render อัตโนมัติเมื่อ `initLiff()` resolve

### Step 2: Add close button to `FrontofficePageHeader.vue`
- [x] Import `useLiff`
- [x] Auto-detect จาก `isInClient` — ไม่ต้อง prop `showClose`
- [x] แสดงปุ่ม X (inline SVG, ไม่ใช้ icon library) ที่มุมขวาบน เมื่อ `isInClient === true`
- [x] On click → call `closeWindow()`
- [x] **Three-way priority (ขวาสุด):**
  1. `isInClient === true` → render X button (hide `showHome` + hide `#actions` slot)
  2. `#actions` slot ถูกส่งมา → render slot (hide `showHome`)
  3. `showHome === true` → render หน้าแรก button
- [x] Fix latent bug: `IntroductionDocumentView.vue` ต้องส่ง `:showHome="false"` เพื่อให้ slot `#actions` render

### Step 3: Verify behavior
- [x] Test บน Android LINE app → ปุ่ม X แสดง → กดแล้วปิด browser กลับ chat
- [x] Test บน iOS LINE app → ปุ่ม X แสดง (`isInClient === true` ทั้งสอง platform)
- [x] Test บน browser ปกติ (Chrome desktop) → ปุ่ม X ไม่แสดง (`isInClient === false`)
- [x] Test mock mode (`VITE_USE_MOCK_LIFF=true`) → `isInClient === false`, closeWindow no-op
- [x] Test `IntroductionDocumentView.vue` → print button แสดง และ X button ซ่อน (เพราะ mock)
- [x] Syntax check: `useLiff.js` pass `node -c`, Vue files pass `vue-tsc --noEmit`

---

## 🧪 4. Test Scenarios

| Environment | `isInClient` | Expected Button (ขวาสุด) |
|-------------|-------------|----------------|
| LINE Android (LIFF) | `true` | X button → closes window |
| LINE iOS (LIFF) | `true` | X button → closes window |
| Chrome desktop | `false` | หน้าแรก button (ถ้า `showHome=true`) |
| Safari mobile (standalone) | `false` | หน้าแรก button (ถ้า `showHome=true`) |
| LIFF mock mode (`VITE_USE_MOCK_LIFF=true`) | `false` | หน้าแรก button (ถ้า `showHome=true`) |

---

## 📁 5. Files to Modify

| File | Change |
|------|--------|
| `csr-frontend/src/composables/useLiff.js` | Hoist refs to module scope; add `isInClient` ref + `closeWindow()` fn |
| `csr-frontend/src/components/FrontofficePageHeader.vue` | Add X button with isInClient auto-detect; fix right-slot priority |
| `csr-frontend/src/views/IntroductionDocumentView.vue` | Add `:showHome="false"` prop so `#actions` slot renders |

---

## 🚫 6. Out of Scope

- ไม่เปลี่ยน backoffice header (backoffice ไม่ได้เปิดจาก LINE)
- ไม่เพิ่ม toast/banner บอกให้กด system back (ใช้ปุ่ม X แทน)
- ไม่แก้ไข routing หรือ navigation logic
- ไม่เปลี่ยน `liff.closeWindow()` post-condition (already gated by `isInClient`)
