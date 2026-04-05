# 🛠️ Phase 8: Staff/Backoffice Plan
**Status:** In Progress (First Slice Delivered)
**Goal:** วางแผนหน้าฝั่ง backoffice สำหรับผู้มีสิทธิ์ภายใน เช่น **ครู** และ **เจ้าหน้าที่เครือข่ายผู้ปกครอง** ให้สามารถดู ค้นหา ตรวจสอบ และจัดการข้อมูลนักเรียน/ผู้ปกครองได้ตามสิทธิ์ โดยออกแบบให้สอดคล้องกับ security model ของระบบ ไม่เปิดข้อมูลส่วนบุคคลเกินสิทธิ์ และไม่ทำ UI นำหน้า backend contract

---

## 🎯 1. Problem Statement
ตอนนี้ระบบถูกออกแบบเป็น **self-service สำหรับผู้ปกครอง** เป็นหลัก:
- auth middleware แปลง LINE access token เป็น `LineUserId`
- endpoint หลักที่มีอยู่ เช่น `/api/me`, `/api/me/introduction-document`, `/api/me/student-photo`, `/api/class`
- ยังไม่มีแนวคิด `staff role`, หรือ authorization policy สำหรับ backoffice
- frontend router ปัจจุบันก็ยังไม่มี backoffice area แยก

ดังนั้น ถ้าจะให้ **ครู** และ **เจ้าหน้าที่เครือข่ายผู้ปกครอง** เข้าดูข้อมูลได้จริง ต้องหยุดเรียกทุกคนว่า `staff` แบบเหมารวมก่อน เพราะความต้องการนี้คือ **หลายบทบาท (multi-role backoffice)** ไม่ใช่ role เดียว

ถ้าทำหน้า backoffice เลยโดยไม่ออกแบบสิทธิ์ก่อน จะเกิดปัญหาตรง ๆ:
- UI เรียกข้อมูลลึกไม่ได้ เพราะ backend ไม่มี endpoint รองรับ
- ถ้าฝืนเปิด endpoint แบบ query ด้วย `studentId` ตรง ๆ จะเสี่ยง PDPA และ privilege escalation
- หน้า backoffice จะกลายเป็นแค่ mock dashboard ที่ยังใช้จริงไม่ได้

ข้อสรุปแบบตรงไปตรงมา:
- **งานหน้านี้ไม่ใช่งาน UI อย่างเดียว**
- ต้องออกแบบ `staff auth + role-based authorization + backoffice API + audit/security rule` ควบคู่กันตั้งแต่ต้น

---

## 🧩 2. Recommended Staff Scope (First Useful Version)
สำหรับรอบแรก ไม่ควรยัดทุกอย่างเข้ามาพร้อมกัน ควรทำเป็น backoffice ที่ใช้งานได้จริงก่อน โดย scope ที่คุ้มสุดคือ:

### **A: Backoffice dashboard**
แสดงภาพรวมระบบ เช่น:
- จำนวนนักเรียนทั้งหมด
- จำนวนรายการที่ลงทะเบียนแล้ว
- จำนวนรายการที่ยังข้อมูลไม่ครบ
- จำนวนรายการที่มีรูปนักเรียน/ผู้ปกครองไม่ครบ

### **B: Student registry list**
หน้ารายการนักเรียนสำหรับครู/เจ้าหน้าที่:
- ตารางรายชื่อนักเรียน
- ค้นหาด้วยชื่อ, รหัสนักเรียน, ห้อง, เลขที่
- filter สถานะ เช่น ลงทะเบียนแล้ว / ยังไม่ครบ / มีปัญหารูปภาพ
- pagination หรือ infinite list ถ้าข้อมูลเริ่มเยอะ

### **C: Student detail / review page**
หน้าเปิดดูข้อมูลรายคน:
- ข้อมูลนักเรียนเต็ม
- ข้อมูลผู้ปกครอง 1 และ 2
- รูปนักเรียนและรูปผู้ปกครองผ่าน protected staff endpoints
- สถานะข้อมูลสำคัญ เช่น ขาด field ไหนบ้าง
- ลิงก์ไปยัง introduction document view ของรายการนั้น

### **D: Data quality actions**
action ขั้นต่ำที่แนะนำ:
- ครูหรือเจ้าหน้าที่ที่มีสิทธิ์สามารถเปลี่ยนสถานะข้อมูล เช่น `Pending`, `Verified`, `Incomplete`
- บันทึก staff/internal note ภายในสำหรับ staff
- mark ว่าตรวจเอกสารแล้ว

### **E: Staff document access**
สำหรับดู/พิมพ์เอกสารรายคน:
- preview ใบแนะนำตัวจากมุมมอง backoffice
- print/export PDF ทีละคน

---

## 🚫 3. Out of Scope for First Pass
ของพวกนี้ยังไม่ควรรีบทำในรอบแรก:
- batch export ทั้งห้อง
- bulk edit หลายคนพร้อมกัน
- role matrix ที่ซับซ้อนเกินจำเป็นหลายชั้น เช่น super-admin / principal / teacher / committee / readonly / auditor พร้อมกันทั้งหมด
- analytics หนัก ๆ หรือ chart เยอะเกินจำเป็น
- realtime collaboration
- public share link สำหรับเอกสาร

ถ้าทำพร้อมกันหมด งานจะบานและ security จะหลุดง่าย

---

## 🔒 4. Required Security Model
นี่คือหัวใจของแผน เพราะระบบนี้มีข้อมูลส่วนบุคคลจริง

### **A: Staff identity source**
ต้องตัดสินใจก่อนว่า **ครู** และ **เจ้าหน้าที่เครือข่ายผู้ปกครอง** login จากอะไร

ตัวเลือกที่ practical สุดตอนนี้:
1. ใช้ LINE เหมือนเดิม แต่มี allowlist ของ `LineUserId` ที่เป็น staff
2. เพิ่มตาราง `StaffUsers` ใน backend
3. middleware/auth layer ต้องแยกได้ว่า request นี้เป็น `guardian`, `teacher`, หรือ `parent-network-staff`

ข้อแนะนำ:
- ถ้าต้องการไปต่อเร็วและไม่เปลี่ยน auth flow มาก ให้ใช้ **LINE + staff allowlist** ก่อน
- แต่ห้าม hardcode ไว้กระจายหลายไฟล์ ควรมี repository/service กลางสำหรับเช็กสิทธิ์

### **B: Minimum role model**
รอบแรกควรใช้ model ที่เรียบแต่ไม่มั่ว:
- `Teacher`
- `ParentNetworkStaff`
- optional ในอนาคต: `SystemAdmin`

ข้อแนะนำแบบตรง ๆ:
- ถ้าความต้องการตอนนี้มีแค่ “เข้าดูข้อมูลได้” อย่าเพิ่งให้ทั้งสอง role แก้ทุกอย่าง
- ควรเริ่มที่ **read access เหมือนกันก่อน** แล้วค่อยเพิ่มสิทธิ์ update เฉพาะบาง role
- ถ้าเจ้าหน้าที่เครือข่ายผู้ปกครองจะเห็นข้อมูลเต็ม ต้องมีการอนุมัติทาง business/PDPA ชัดเจน ไม่ใช่ถือว่าดูได้เองโดยอัตโนมัติ

### **C: Authorization rule**
ควรมี policy ชัดเจน:
- guardian เข้าถึงได้เฉพาะ `/api/me*`
- staff เข้าถึงได้เฉพาะ `/api/backoffice/*` ที่มี policy แยก
- ห้าม reuse endpoint เดิมแบบเปิด query parameter ให้ staff ใช้ปนกับ guardian

### **D: Protected media rule**
รูปภาพห้ามเปิด public เหมือนเดิม

สำหรับ staff ต้องมี endpoint แยก เช่น:
- `GET /api/backoffice/students/{studentId}/student-photo`
- `GET /api/backoffice/students/{studentId}/guardians/{guardianOrder}/photo`

ทุก endpoint ต้องเช็ก role-based authorization ก่อนเสมอ

### **E: Auditability**
ถ้าจะเปิดสิทธิ์ให้ครูและเจ้าหน้าที่เห็นข้อมูลเต็ม ควรเตรียมเรื่อง audit ตั้งแต่แผน:
- ใครเปิดดูข้อมูลใคร
- ใครเปลี่ยน status
- ใครบันทึก staff/internal note

รอบแรกอาจยังไม่ต้องมี audit table เต็มรูปแบบ แต่ควรวาง interface/service เผื่อไว้

---

## ⚙️ 5. Backend Work Required Before UI

### **A: New staff domain model**
ไฟล์/แนวคิดที่ควรเพิ่ม:
- model สำหรับ staff identity
- service สำหรับเช็กว่า `LineUserId` นี้เป็น `Teacher` หรือ `ParentNetworkStaff`
- optional: role enum เช่น `Teacher`, `ParentNetworkStaff`, `SystemAdmin`

### **B: Backoffice-specific DTOs**
ไม่ควรใช้ DTO ของ `/api/me` ตรง ๆ ทั้งหมด เพราะบริบทคนละแบบ

DTO ที่ควรมี:
- `BackofficeDashboardSummaryResponse`
- `BackofficeStudentListItemResponse`
- `BackofficeStudentDetailResponse`
- `BackofficeStudentStatusUpdateRequest`
- `BackofficeStudentInternalNoteUpdateRequest` หรือโครงสร้าง note ที่เหมาะสม

### **C: New repository methods**
repository ตอนนี้ยังเน้น self-service และ list แบบกว้าง ๆ
ต้องเพิ่ม method ที่ตอบโจทย์ backoffice เช่น:
- list students แบบ filter/search/sort
- get student พร้อม guardians แบบเต็ม
- update status
- save staff/internal note
- summary counts สำหรับ dashboard

### **D: New backoffice endpoints**
แนะนำให้แยก namespace ชัดเจน:
- `GET /api/backoffice/dashboard`
- `GET /api/backoffice/students`
- `GET /api/backoffice/students/{id}`
- `PATCH /api/backoffice/students/{id}/status`
- `PUT /api/backoffice/students/{id}/note`
- `GET /api/backoffice/students/{id}/document`
- `GET /api/backoffice/students/{id}/student-photo`
- `GET /api/backoffice/students/{id}/guardians/{guardianOrder}/photo`

### **E: Validation and error contract**
backoffice endpoint ควรคืน error แบบชัด:
- `401` เมื่อไม่มี token หรือ token ใช้ไม่ได้
- `403` เมื่อ login ได้แต่ไม่มีสิทธิ์ role ที่ต้องการ
- `404` เมื่อไม่พบ student
- `400` เมื่อ filter/status ไม่ถูกต้อง

ตรงนี้สำคัญ เพราะ UX ฝั่ง backoffice จะพังง่ายถ้า error contract มั่ว

---

## 🧱 6. Suggested Backoffice Frontend Information Architecture

### **A: Route structure**
ควรแยก route ชัดเจน เช่น:
- `/backoffice`
- `/backoffice/students`
- `/backoffice/students/:id`

ถ้ามีหน้าเอกสารจากฝั่ง backoffice:
- `/backoffice/students/:id/document`

### **B: Layout structure**
ควรมี `BackofficeLayout` แยกจาก `MainLayout` ของผู้ปกครอง เพราะ navigation และ mental model คนละโลก

layout ที่ควรมี:
- top bar แสดงชื่อระบบและ staff identity
- side nav หรือ top tabs สำหรับ desktop/tablet
- mobile fallback ถ้าจำเป็น

### **C: Core screens**
1. `BackofficeDashboardView.vue`
2. `BackofficeStudentListView.vue`
3. `BackofficeStudentDetailView.vue`
4. `BackofficeStudentDocumentView.vue`

### **D: Reusable components**
เพื่อไม่ให้ view ใหญ่เกินไป:
- `BackofficeStatCard.vue`
- `BackofficeStudentFilters.vue`
- `BackofficeStudentTable.vue`
- `BackofficeStudentProfileCard.vue`
- `BackofficeGuardianCard.vue`
- `BackofficeStatusBadge.vue`
- `BackofficeNoteEditor.vue`

### **E: Composables / services**
แยก logic ให้ test ได้ง่าย:
- `useBackofficeAuth.js`
- `useBackofficeDashboard.js`
- `useBackofficeStudentList.js`
- `useBackofficeStudentDetail.js`
- `backofficeApi.js`

---

## 🔄 7. Data Flow Recommendation

### **A: Staff access flow**
1. frontend init LIFF
2. เรียก endpoint ตรวจสิทธิ์ staff หรือ endpoint dashboard โดยตรง
3. ถ้าได้ `403` ให้ redirect ไปหน้า not-authorized หรือหน้าหลัก
4. ถ้าได้สิทธิ์ ค่อย mount backoffice routes

### **B: Student list flow**
1. staff เปิด `/backoffice/students`
2. frontend ส่ง query เช่น `search`, `room`, `status`, `page`
3. backend query repository แบบ filtered
4. backend คืน list item DTO ที่พอสำหรับตาราง ไม่ส่ง detail ทุกอย่างมาทีเดียว

### **C: Student detail flow**
1. staff กดเปิดรายคน
2. frontend เรียก `GET /api/backoffice/students/{id}`
3. backend รวม student + guardians + status + note
4. frontend render เป็น section ย่อย

### **D: Photo/document flow**
1. detail page แสดง URL ของ backoffice photo endpoints
2. browser โหลดรูปผ่าน token แบบเดียวกับ API call
3. document view ใช้ backoffice document endpoint แทน `/api/me/introduction-document`

---

## 🗃️ 8. Data Model Gaps That Will Affect Backoffice Page
หน้าฝั่ง backoffice จะชนข้อจำกัดของ model ปัจจุบันชัดกว่าหน้า guardian อีก

### **A: ยังไม่มี staff table / role store**
ต้องเพิ่มจริง ไม่งั้นไม่มีทางแยก guardian, teacher, และ parent-network staff อย่างปลอดภัย

### **B: Students table มี `Status` แล้ว แต่ semantics ยังไม่ชัด**
ตอนนี้มี field `Status` ใน `Students` แต่ความหมายยังไม่ชัดว่าเป็น:
- สถานะการเรียน
- สถานะการลงทะเบียน
- สถานะการตรวจสอบข้อมูล

ถ้าจะใช้ใน backoffice ต้องแยกให้ชัด ไม่งั้นข้อมูลจะปนกันมั่ว

ข้อแนะนำ:
- ถ้า `Status` ใช้เรื่องการเรียนอยู่แล้ว ให้เพิ่ม field ใหม่ เช่น `RegistrationStatus` หรือ `ReviewStatus`
- อย่ายัดหลายความหมายใน field เดียว

### **C: ยังไม่มี staff note field/table**
ถ้าจะให้ staff จด note ภายใน ต้องเพิ่ม field หรือตารางใหม่ เช่น:
- `StaffNotes`
- หรือ field note ง่าย ๆ ที่ `Students`

แต่ถ้าคาดว่าจะมีประวัติหลายครั้ง ควรเป็น table แยกตั้งแต่แรก

### **D: Contact/committee data ยังเป็น static UI**
ถ้าหน้า backoffice จะดูแลข้อมูลครู/กรรมการจริง ต้องมี API/CRUD แยกใน phase ต่อไป ไม่ใช่ใช้ข้อมูล hardcoded ใน `Contacts.vue`

---

## 🛠️ 9. File-Level Implementation Checklist

### **A: Backend auth / authorization**
- [x] เพิ่ม model/table สำหรับ `StaffUsers` หรือ allowlist ที่แยก role ได้
- [x] เพิ่ม service สำหรับตรวจสิทธิ์ `Teacher` และ `ParentNetworkStaff` จาก `LineUserId`
- [x] เพิ่ม helper/policy สำหรับป้องกัน `/api/backoffice/*`
- [x] แยก `401` กับ `403` ให้ชัด

### **B: Backend repository / service**
- [x] เพิ่ม query list สำหรับ backoffice
- [x] เพิ่ม query detail สำหรับ student + guardians
- [x] เพิ่ม summary query สำหรับ dashboard
- [x] เพิ่ม update status flow
- [x] เพิ่ม save note flow
- [x] เพิ่ม document + photo access flow สำหรับ backoffice

### **C: Backend endpoint layer**
- [x] เพิ่ม `/api/backoffice/dashboard`
- [x] เพิ่ม `/api/backoffice/students`
- [x] เพิ่ม `/api/backoffice/students/{id}`
- [x] เพิ่ม endpoint update status/note
- [x] เพิ่ม backoffice photo endpoints
- [x] เพิ่ม backoffice document endpoint

### **D: Frontend routing / layout**
- [x] เพิ่ม backoffice route group ใน `csr-frontend/src/router/index.js`
- [x] เพิ่ม `BackofficeLayout.vue`
- [x] เพิ่ม guard สำหรับ route backoffice (จัดการผ่าน API & component error handling)
- [x] เพิ่ม unauthorized/not-found handling สำหรับ backoffice area

### **E: Frontend screens**
- [x] เพิ่ม `BackofficeDashboardView.vue`
- [x] เพิ่ม `BackofficeStudentListView.vue`
- [x] เพิ่ม `BackofficeStudentDetailView.vue`
- [ ] เพิ่ม `BackofficeStudentDocumentView.vue`

### **F: Frontend state / API client**
- [x] เพิ่ม `src/services/backofficeApi.js`
- [x] เพิ่ม logic สำหรับ dashboard/list/detail
- [x] จัดการ loading/error/empty state ครบทุกหน้า
- [x] แยก layout และ views ให้ maintain ง่าย

### **G: Verification**
- [x] ทดสอบว่า guardian user เปิด `/api/backoffice/*` ไม่ได้
- [x] ทดสอบว่า teacher ดู list/detail ได้จริงตามสิทธิ์
- [x] ทดสอบ filter/search
- [x] ทดสอบกรณีข้อมูลไม่ครบ, ไม่มีรูป, guardian คนที่ 2 ไม่มีข้อมูล
- [x] ทดสอบว่า protected photo security ไม่ถอยหลัง

---

## 🧪 10. Acceptance Criteria
- มี backoffice route และ layout แยกจากฝั่งผู้ปกครองชัดเจน
- มีระบบตรวจสิทธิ์ `Teacher` และ `ParentNetworkStaff` จริง ไม่ใช่แค่ซ่อนปุ่มหน้า UI
- staff ดูรายการนักเรียนแบบค้นหา/filter ได้
- staff เปิดดูรายละเอียดรายคนได้ครบทั้ง student และ guardians ตามสิทธิ์
- staff เปิดดูรูปผ่าน protected backoffice endpoints ได้
- staff เปิดดู/พิมพ์ document รายคนได้
- มีการอัปเดตสถานะข้อมูลอย่างน้อย 1 ประเภทที่ใช้จริงได้สำหรับ role ที่ได้รับสิทธิ์
- guardian ไม่มีสิทธิ์เรียก backoffice endpoints
- frontend logic ไม่กระจุกเป็น view ยาว ๆ และแยก component/composable ตามหน้าที่

---

## 🧭 11. Recommended Delivery Order
- [x] 1. ออกแบบ staff identity + authorization model สำหรับ `Teacher` และ `ParentNetworkStaff`
- [x] 2. เพิ่ม backoffice endpoint ชุดขั้นต่ำสำหรับ dashboard/list/detail
- [x] 3. ทำ backoffice layout และ route guard
- [x] 4. ทำ student list page พร้อม search/filter
- [x] 5. ทำ student detail page (รองรับ document/photo อย่างปลอดภัย)
- [x] 6. เพิ่ม status update และ internal note สำหรับ role ที่ได้รับสิทธิ์แก้ไข
- [ ] 7. ค่อยขยายไป CRUD ข้อมูลครู/กรรมการ หรือ batch features

---

## 💥 12. Honest Risks / Caveats
- ถ้ายังไม่เคลียร์ว่า `Teacher` กับ `ParentNetworkStaff` เห็นข้อมูลได้เท่ากันไหม ระบบนี้จะเดินต่อไม่ได้อย่างปลอดภัย
- ถ้าเอา `/api/me` ไปดัดใช้เป็น backoffice endpoint จะเละทั้ง security และ maintainability
- ถ้าใช้ field `Status` เดิมแบบไม่แยก semantics จะสร้างหนี้เทคนิคแน่นอน
- ถ้าทำ document/photo access ให้ staff โดยไม่แยก authorization layer จะเป็นช่องโหว่ทันที
- ถ้าเจ้าหน้าที่เครือข่ายผู้ปกครองไม่ควรเห็นข้อมูลเต็มทุกช่อง ต้องทำ field-level masking/limited view เพิ่ม ไม่งั้น requirement จะขัด PDPA เอง
- ถ้าพยายามทำ dashboard + list + detail + contacts CRUD + export batch พร้อมกัน จะช้าและพังง่าย

---

## ✅ 13. Suggested First Implementation Slice
ถ้าจะเริ่มแบบคุ้มที่สุด ผมแนะนำ slice แรกนี้ (ดำเนินการเสร็จสิ้นแล้ว):
- [x] staff allowlist จาก `LineUserId` พร้อม role ขั้นต่ำ `Teacher` และ `ParentNetworkStaff`
- [x] `GET /api/backoffice/dashboard`
- [x] `GET /api/backoffice/students`
- [x] `GET /api/backoffice/students/{id}`
- [x] backoffice routes + `BackofficeLayout`
- [x] `BackofficeDashboardView.vue`
- [x] `BackofficeStudentListView.vue`
- [x] `BackofficeStudentDetailView.vue`

slice นี้เล็กพอจะส่งมอบได้จริง และเป็นฐานให้ document/status/note ต่อได้โดยไม่ต้องรื้อใหม่

---

## ❓ 14. Open Business Questions That Must Be Answered
- ครูเห็นข้อมูลเต็มทุก field ได้ทั้งหมดหรือไม่
- เจ้าหน้าที่เครือข่ายผู้ปกครองเห็นข้อมูลเต็มเท่าครู หรือเห็นเฉพาะบาง field
- ทั้งสอง role แก้สถานะ/บันทึก note ได้เหมือนกันหรือไม่ หรือให้ดูได้อย่างเดียวก่อน
- การเข้าถึง document และรูปถ่าย ให้ทั้งสอง role เท่ากันหรือไม่

ถ้ายังไม่ตอบ 4 ข้อนี้ให้ชัด implementation จะเดาเอง และนั่นเป็นวิธีที่ไม่ควรทำ
