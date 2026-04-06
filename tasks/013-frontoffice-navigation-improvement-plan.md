# 🧭 Phase 10: Front Office Navigation Improvement Plan
**Status:** In Progress — First slice implemented
**Depends on:** existing front-office routes and views in `csr-frontend`
**Goal:** ปรับปรุง navigation ของหน้าฝั่งผู้ปกครอง (front office) ให้ผู้ใช้สามารถย้อนกลับหรือกลับหน้าแรกได้อย่างชัดเจนและสม่ำเสมอ โดยแก้ที่ route/layout/header architecture ไม่ใช่แค่แปะปุ่มเฉพาะหน้า

---

## 🎯 1. Problem Statement
ตอนนี้หน้า front office ยังมี navigation experience ที่ไม่สม่ำเสมอ โดยเฉพาะหน้าที่เป็น standalone page เช่น:
- `/register`
- `/profile/edit`
- `/document`

หน้าพวกนี้มี local header ของตัวเอง แต่ **ไม่มีปุ่ม back** และ **ไม่มีทางลัดกลับหน้าแรก** อย่างชัดเจน

ขณะเดียวกัน หน้าในกลุ่มหลักอย่าง:
- `/dashboard`
- `/class-list`
- `/contacts`

อยู่ใต้ `MainLayout` และมี bottom navigation อยู่แล้ว

ดังนั้นปัญหาจริงไม่ใช่แค่ลืมใส่ปุ่ม แต่เป็นปัญหาเชิงโครงสร้าง:
- standalone routes อยู่นอก `MainLayout`
- header behavior ไม่ได้ถูกออกแบบเป็นระบบเดียวกัน
- ผู้ใช้สามารถเข้าไปติดอยู่ใน flow ของหน้าแบบฟอร์ม/เอกสารโดยไม่รู้จะกลับ home ยังไง

---

## 🔍 2. Current Findings From Code

### **A: Current route structure**
จาก `csr-frontend/src/router/index.js`:
- `/register`, `/profile/edit`, `/document` เป็น top-level route
- `/dashboard`, `/class-list`, `/contacts` อยู่ใต้ `MainLayout`

ผลคือ:
- หน้า standalone ไม่ได้ bottom nav
- navigation behavior แตกต่างกันตาม route structure ไม่ใช่ตาม UX intent

### **B: MainLayout already provides bottom navigation**
จาก `csr-frontend/src/components/MainLayout.vue`:
- มี bottom nav ไป `หน้าแรก`, `เพื่อน`, `ติดต่อ`
- เหมาะกับหน้าหลักที่เป็น browse/navigation mode

### **C: Standalone pages have isolated headers**
จาก `Register.vue` และ `IntroductionDocumentView.vue`:
- มี local header ของตัวเอง
- ยังไม่มี `back` action
- ยังไม่มี `home` action
- ยังไม่มี fallback logic ถ้าเปิดหน้าตรงจาก deep link หรือ redirect flow

### **D: Real UX consequence**
ผู้ใช้ที่อยู่ในหน้า:
- ลงทะเบียนข้อมูล
- แก้ไขข้อมูล
- ดูเอกสาร

อาจไม่รู้ว่าจะกลับหน้าแรกตรงไหน โดยเฉพาะบนมือถือที่ expectation คือควรมีปุ่มย้อนกลับที่มองเห็นชัด

---

## 💥 3. Root Cause
root cause คือ **navigation responsibilities ถูกกระจายผิดที่**

ตอนนี้ระบบแบ่งแบบนี้:
- บางหน้าใช้ `MainLayout`
- บางหน้าใช้ local header แบบ ad hoc

ปัญหาคือไม่มี abstraction กลางสำหรับหน้าประเภท:
- main tab page
- form/detail page
- document page

ผลคือแต่ละหน้าแก้ header/navigation เอง และสุดท้าย UX ไม่คงที่

สรุปแบบตรง ๆ:
- ถ้าแค่เพิ่มปุ่มกลับใน `Register.vue` หน้าเดียว งานนี้ยังไม่จบ
- ต้องออกแบบ navigation pattern ของ front office ใหม่ให้ครบทั้งชุด

---

## 🧩 4. Navigation Design Principles

### **A: Every non-home page must have a clear exit path**
ทุกหน้าที่ไม่ใช่หน้า home/dashboard ต้องมีอย่างน้อยหนึ่งทางออกที่ชัดเจน:
- `Back`
- หรือ `Home`
- หรือมีทั้งสองอย่างถ้าเป็นหน้า flow สำคัญ

### **B: Back must be safe**
การกด back ห้ามพึ่ง `router.back()` อย่างเดียว เพราะบางกรณีผู้ใช้เข้าหน้านั้นจาก:
- deep link
- page refresh
- redirect จาก auth flow

ดังนั้นต้องมี fallback เช่น:
- ถ้ามี history ค่อย back
- ถ้าไม่มี ให้ไป `/dashboard`

### **C: Home destination must be explicit**
คำว่า home ของ front office ควรหมายถึง:
- `/dashboard`

ไม่ควรปล่อยให้แต่ละหน้าตีความเอง

### **D: Header behavior should be standardized**
หน้าฝั่งผู้ปกครองควรใช้ header pattern เดียวกันตามประเภทของหน้า ไม่ใช่แต่ละหน้าเขียนปุ่มเอง

---

## 🧱 5. Proposed Navigation Architecture

### **A: Keep MainLayout for tab-style pages**
ให้ `MainLayout` รับผิดชอบหน้าที่เป็น top-level browsing pages ต่อไป:
- `Dashboard`
- `ClassList`
- `Contacts`

### **B: Add a reusable front-office page header component**
ควรเพิ่ม component กลาง เช่น:
- `FrontofficePageHeader.vue`

ความสามารถขั้นต่ำ:
- title
- subtitle optional
- `showBack`
- `showHome`
- `backFallbackRoute`
- optional action slot ด้านขวา

### **C: Use the shared header in standalone pages**
นำ component นี้ไปใช้กับ:
- `Register.vue`
- `IntroductionDocumentView.vue`
- หน้า form/detail อื่นในอนาคต

### **D: Optional next step: shared subpage layout**
ถ้าหน้า standalone เริ่มเยอะ ควรเพิ่ม layout ใหม่ เช่น:
- `FrontofficeSubpageLayout.vue`

หน้าที่ของ layout นี้:
- render shared header
- กำหนด safe area/padding
- optional sticky action bar
- ไม่ต้องมี bottom nav ถ้า UX ไม่เหมาะ

ข้อแนะนำ:
- รอบแรกไม่จำเป็นต้องรีแฟกเตอร์ทุกหน้าไป layout ใหม่ทันที
- แต่ควรออกแบบ component header ให้ reuse ได้ก่อน

---

## 🧭 6. Page-by-Page Navigation Rules

### **A: Dashboard (`/dashboard`)**
- ถือเป็น home screen
- ไม่ต้องมี back button
- มี bottom nav ตามเดิม

### **B: Class list (`/class-list`)**
- ยังใช้ bottom nav ได้
- ไม่จำเป็นต้องมี back button ถ้ายังถือเป็น top-level tab

### **C: Contacts (`/contacts`)**
- ยังใช้ bottom nav ได้
- ไม่จำเป็นต้องมี back button ถ้ายังถือเป็น top-level tab

### **D: Register (`/register`)**
ควรมี:
- ปุ่ม `กลับ`
- ปุ่ม `หน้าแรก`

behavior ที่แนะนำ:
- `Back`: ย้อน history ถ้ามี, ไม่งั้นไป `/dashboard`
- `Home`: ไป `/dashboard`

### **E: Edit Profile (`/profile/edit`)**
ควรมี:
- ปุ่ม `กลับ`
- ปุ่ม `หน้าแรก`

behavior ที่แนะนำ:
- `Back`: กลับ `/dashboard` หรือ safe back พร้อม fallback `/dashboard`
- หลังบันทึกสำเร็จกลับ `/dashboard`

### **F: Introduction Document (`/document`)**
ควรมี:
- ปุ่ม `กลับ`
- ปุ่ม `หน้าแรก`
- ปุ่ม `พิมพ์ / บันทึก PDF` ตามเดิม

behavior ที่แนะนำ:
- `Back`: กลับ `/dashboard`
- `Home`: ไป `/dashboard`

หมายเหตุ:
- หน้าเอกสารเป็นหน้าที่ผู้ใช้อาจเปิดแบบตรงหรือเปิดใหม่ใน browser tab ได้ จึงไม่ควรพึ่ง history อย่างเดียว

---

## 🖥️ 7. Wireframes

### **A: Shared subpage header**
```text
+--------------------------------------------------------------------------------+
| [← กลับ]                     แก้ไขข้อมูล                      [⌂ หน้าแรก]     |
+--------------------------------------------------------------------------------+
```

### **B: Register page**
```text
+--------------------------------------------------------------------------------+
| [← กลับ]                   ลงทะเบียนข้อมูล                    [⌂ หน้าแรก]     |
+--------------------------------------------------------------------------------+
|                                                                            |
|  ฟอร์มข้อมูลนักเรียน / ผู้ปกครอง                                           |
|                                                                            |
|  [บันทึกข้อมูล]                                                            |
+--------------------------------------------------------------------------------+
```

### **C: Document page**
```text
+--------------------------------------------------------------------------------+
| [← กลับ]            เอกสารแนะนำนักเรียนและครอบครัว        [⌂ หน้าแรก]       |
+--------------------------------------------------------------------------------+
|                                                        [พิมพ์ / บันทึก PDF] |
|                                                                            |
|  แสดงเอกสาร                                                                |
|                                                                            |
+--------------------------------------------------------------------------------+
```

### **D: Limited-history fallback behavior**
```text
User taps [← กลับ]
  -> if browser history exists: navigate back
  -> else: navigate to /dashboard
```

---

## ⚙️ 8. Implementation Strategy

### **A: Router metadata (recommended)**
ควรเพิ่ม `meta` ให้ route ที่ต้องการ header behavior ชัดเจน เช่น:
- `requiresFrontofficeHeader`
- `showBackButton`
- `showHomeButton`
- `backFallbackRoute`
- `pageTitle`

ข้อดี:
- logic ไม่ไปฝังอยู่ในแต่ละ view
- maintain ง่ายขึ้นเมื่อมีหน้าเพิ่ม

### **B: Navigation helper**
ควรมี helper/composable กลาง เช่น:
- `useSafeNavigation()`

หน้าที่:
- `goBackOrFallback(fallbackRoute)`
- `goHome()`

### **C: Incremental rollout**
ลำดับ implement ที่เหมาะ:
1. สร้าง shared header component
2. สร้าง safe navigation helper
3. ใช้กับ `Register.vue`
4. ใช้กับ `IntroductionDocumentView.vue`
5. ใช้กับ `EditProfile` flow
6. ค่อยพิจารณารีแฟกเตอร์เป็น subpage layout ถ้าจำเป็น

---

## 🧪 9. Acceptance Criteria
- ทุกหน้า front office ที่ไม่ใช่ home มีทางกลับหรือกลับหน้าแรกอย่างชัดเจน
- `Register` มีปุ่ม back และ home
- `Edit Profile` มีปุ่ม back และ home
- `Document` มีปุ่ม back และ home โดยไม่ชนกับปุ่ม print
- การกด back ทำงานได้แม้ไม่มี browser history โดย fallback ไป `/dashboard`
- navigation pattern ของ front office สม่ำเสมอ ไม่ใช่แต่ละหน้าออกแบบ header เอง
- ไม่มีการพึ่ง bottom nav ในหน้าที่เป็น standalone form/document page
- UX บนมือถือใช้งานได้จริงและไม่ทำให้ผู้ใช้ติดหน้า

---

## 🛠️ 10. File-Level Implementation Checklist

### **A: Router**
- [x] ทบทวน route meta สำหรับ front-office standalone pages
- [x] กำหนด canonical home route เป็น `/dashboard`
- [x] เพิ่ม metadata สำหรับ title/back/home behavior ถ้าจะใช้ approach นี้

### **B: Shared navigation primitives**
- [x] เพิ่ม `FrontofficePageHeader.vue`
- [x] เพิ่ม `useSafeNavigation.js` หรือ helper กลางที่ test ได้ง่าย
- [x] หลีกเลี่ยง logic ซ้ำใน `Register.vue` และ `IntroductionDocumentView.vue`

### **C: Page updates**
- [x] ปรับ `Register.vue` ให้ใช้ shared header
- [x] ปรับ `EditProfile` flow ให้ใช้ shared header เดียวกัน
- [x] ปรับ `IntroductionDocumentView.vue` ให้มี back/home ที่ไม่ชนกับ print action
- [x] ตรวจดูว่ามีหน้า front-office อื่นนอก `MainLayout` อีกหรือไม่

### **D: UX polish**
- [ ] ปุ่ม back/home ต้องเห็นชัดบน mobile
- [ ] ข้อความ title ต้องไม่ชนกับ action buttons
- [ ] ปุ่ม print ยังใช้งานสะดวกในหน้า document
- [ ] sticky header behavior ต้องไม่กินพื้นที่จนใช้งานลำบาก

### **E: Verification**
- [ ] ทดสอบเปิด `/register` ตรง ๆ แล้วกลับหน้าแรกได้
- [ ] ทดสอบเข้า `/profile/edit` จาก dashboard แล้วกลับได้ถูก
- [ ] ทดสอบเปิด `/document` แล้วกลับหน้า dashboard ได้
- [ ] ทดสอบ reload หน้า standalone แล้วปุ่ม back ยังไม่พัง
- [ ] ทดสอบบนมือถือว่าปุ่มไม่เล็กหรือชิดขอบเกินไป

---

## 🧭 11. Recommended Delivery Order
1. ล็อก navigation rules ของแต่ละหน้าให้ชัด
2. เพิ่ม shared header component
3. เพิ่ม safe navigation helper
4. ปรับ `Register` และ `Edit Profile`
5. ปรับ `Document`
6. ทดสอบ deep-link / refresh / mobile behavior
7. ค่อยพิจารณารีแฟกเตอร์เป็น subpage layout ถ้าหน้า standalone เพิ่มขึ้น

---

## 💥 12. Honest Risks / Caveats
- ถ้าแก้แค่ `Register.vue` หน้าเดียว ปัญหาจะย้ายไปโผล่ที่ `Document` ต่อ
- ถ้าใช้ `router.back()` ตรง ๆ โดยไม่มี fallback มันจะพังเมื่อเปิดหน้าตรงหรือ refresh
- ถ้าใส่ทั้ง bottom nav และ subpage header ในหน้าฟอร์ม อาจรกและทำให้สับสน
- ถ้าแต่ละหน้าเขียน header เองต่อไป UX จะไม่คงที่และ maintain ยาก

---

## ✅ 13. Suggested First Implementation Slice
- เพิ่ม `FrontofficePageHeader.vue`
- เพิ่ม safe back/home navigation helper
- ใช้กับ `Register.vue`
- ใช้กับ `IntroductionDocumentView.vue`
- กำหนดให้ fallback home เป็น `/dashboard`

slice นี้เล็กพอจะส่งมอบได้เร็ว และแก้ปัญหาที่ผู้ใช้เจอจริงทันที โดยไม่ต้องรื้อทั้งระบบในรอบเดียว
