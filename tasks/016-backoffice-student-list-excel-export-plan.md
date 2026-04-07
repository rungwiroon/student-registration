# 📊 Phase 16: Backoffice Student List Excel Export Plan
**Status:** Proposed
**Depends on:** `tasks/010-admin-page-plan.md`, `tasks/011-backoffice-role-access-policy-and-wireframe.md`, existing `/api/backoffice/students` list flow
**Goal:** เพิ่มความสามารถให้ผู้ใช้ฝั่ง backoffice export ข้อมูลนักเรียนจากหน้า `http://localhost:5173/backoffice/students` ออกมาเป็นไฟล์ Excel โดยยึดตามสิทธิ์ของผู้ใช้, ขอบเขตข้อมูลที่อนุญาต, และ filter/search ที่ผู้ใช้เลือกอยู่ในหน้ารายการ

---

## 🎯 1. Problem Statement
ตอนนี้หน้า `backoffice/students` ใช้สำหรับดูรายชื่อนักเรียนและค้นหาข้อมูลเบื้องต้น แต่ยังไม่มีทาง export ออกไปใช้งานต่อในรูปแบบ Excel

ความต้องการนี้ดูเหมือนง่าย แต่จริง ๆ ไม่ใช่งาน “เพิ่มปุ่มดาวน์โหลด” เฉย ๆ เพราะมีเรื่องที่ต้องล็อกก่อน:

- export จะเอาข้อมูลชุดไหนแน่
- export ทั้งหมด หรือ export ตาม filter/search ปัจจุบัน
- role ไหน export ได้บ้าง
- field ไหน export ได้บ้างตาม PDPA และ policy ปัจจุบัน
- จะให้ frontend สร้าง Excel เอง หรือให้ backend เป็นคน generate
- ถ้าข้อมูลในหน้า list กับข้อมูลในไฟล์ไม่ตรงกัน ผู้ใช้จะสับสนทันที

สรุปตรง ๆ:
- งานนี้ต้องเป็น **backend-driven export**
- ต้องผูกกับ **authorization + data contract + filtering behavior** ให้ชัด
- ห้ามทำแบบเอา array ในหน้า Vue มาแปลงเป็น Excel ตรง ๆ แล้วถือว่าจบ

---

## 🔍 2. Current Findings From Codebase

### **A: หน้า `backoffice/students` ยังเป็น list + client-side search**
จาก `csr-frontend/src/views/backoffice/StudentListView.vue`:
- หน้า list โหลดข้อมูลทั้งหมดผ่าน `fetchStudents(token)`
- มี `search` ในหน้า แต่ filtering ปัจจุบันทำฝั่ง client
- ยังไม่มีปุ่ม export

ผลคือ:
- ถ้าจะ export “ตามสิ่งที่ผู้ใช้เห็น” ต้องนิยามให้ชัดว่าฝั่ง backend จะรับ query อะไร
- ถ้าปล่อยให้ frontend export จาก filtered array ใน browser จะไม่เหมาะกับงานที่ข้อมูลเริ่มโต และคุมสิทธิ์ได้ไม่ดีพอ

### **B: endpoint `/api/backoffice/students` มี role-aware response แล้วในระดับหนึ่ง**
จาก `CsrApi/BackofficeEndpoints.cs`:
- `Teacher` ได้ข้อมูลเต็มกว่า
- `ParentNetworkStaff` ได้ข้อมูล limited/masked กว่า

นี่เป็นสัญญาณที่ดี เพราะ export ก็ควรใช้ principle เดียวกัน:
- คนที่เห็นข้อมูลเต็มใน list จึงจะ export ข้อมูลเต็มได้
- คนที่เห็นข้อมูล masked ก็ควร export ได้แค่ masked/limited version เท่านั้น หรืออาจถูก deny ไปเลย

### **C: ตอนนี้ยังไม่มี export endpoint และยังไม่มี Excel package**
จาก `CsrApi.csproj`:
- ยังไม่มี package สำหรับสร้าง `.xlsx`
- backend ยังไม่มี endpoint แนว `/export`

แปลว่างานนี้ต้องเพิ่มทั้ง:
- package สำหรับ Excel generation
- service สำหรับ map/export rows
- endpoint สำหรับ download file
- frontend action เพื่อ trigger download

### **D: Policy ปัจจุบันยัง conservative สำหรับข้อมูลอ่อนไหวบางส่วน**
จาก task policy เดิม:
- `Teacher` เห็นข้อมูลเต็มได้
- `ParentNetworkStaff` ควรเป็น read-only และ limited view เป็น default ถ้า PDPA ยังไม่ clear

ดังนั้น export ต้องไม่วิ่งนำ policy เดิม

---

## 💥 3. Root Cause
ปัญหาจริงไม่ใช่แค่ไม่มีปุ่ม export แต่คือ **ยังไม่มี export contract ที่ผูกกับ role และ list state อย่างถูกต้อง**

ถ้าทำแบบลวก ๆ จะเกิดปัญหาอย่างน้อย 4 แบบ:
- export ได้ข้อมูลเกินสิทธิ์
- export แล้วข้อมูลไม่ตรงกับ filter/search ในหน้า
- export logic ซ้ำกับ list mapping และ drift จากกันในอนาคต
- frontend กลายเป็นที่ประกอบ business logic export เอง ซึ่ง maintain ยาก

---

## 🧭 4. Design Principles

### **A: Export must be server-generated**
ให้ backend เป็นคนสร้างไฟล์ Excel เสมอ

เหตุผล:
- คุม authorization ได้จริง
- คุม field mapping ได้จากศูนย์กลาง
- scale กับข้อมูลจำนวนมากได้ดีกว่า
- ลดความเสี่ยงเรื่อง frontend export ข้อมูลเกินสิทธิ์

### **B: Export must respect current role policy**
อย่างน้อยต้องยึดกติกานี้:
- `Teacher` export ข้อมูลได้ตาม scope ที่ teacher มีสิทธิ์เห็น
- `ParentNetworkStaff` ห้ามได้ข้อมูลมากกว่าที่เห็นในระบบ

ข้อแนะนำแบบตรง ๆ:
- ถ้า policy สำหรับ `ParentNetworkStaff` ยังไม่ชัด ให้เริ่มจาก 1 ใน 2 ทางนี้เท่านั้น:
  - deny export ไปก่อน
  - หรือ export ได้เฉพาะ limited/masked dataset

### **C: Export scope must be explicit**
ต้องระบุให้ชัดว่า export รอบแรกคืออะไร:
- export เฉพาะรายการจากหน้า `student list`
- ไม่ใช่ full detail export ทุก field
- ไม่ใช่ document export
- ไม่ใช่ bulk ZIP/photo export

### **D: Export must follow filter/search state**
ถ้าผู้ใช้ค้นหา/กรองอยู่ แล้วกด export ไฟล์ต้องสะท้อนชุดข้อมูลเดียวกันกับที่ตั้งใจ export

แต่เนื่องจากตอนนี้หน้า list ยัง filter ฝั่ง client เป็นหลัก ข้อแนะนำคือ:
- ถ้าจะทำ export ให้ถูก ควรเริ่มย้าย filtering/search logic ที่สำคัญไปที่ backend query ด้วย
- อย่างน้อย endpoint export ต้องรับ query parameter ชุดเดียวกับ list endpoint ที่หน้าใช้อยู่

### **E: Keep Excel structure simple for first pass**
รอบแรกไม่ควรทำ formatting หรูหราเกินไป

first pass ควรมีแค่:
- worksheet เดียว
- header row ชัดเจน
- data rows ที่สอดคล้องกับ student list / review use case
- filename ที่สื่อความหมาย

---

## 🧩 5. Proposed Scope for First Pass

### **A: Export target**
first pass ให้ export จากหน้า `BackofficeStudentListView` เท่านั้น

### **B: Export format**
- ไฟล์ `.xlsx`
- 1 worksheet
- ชื่อ sheet เช่น `Students`

### **C: Exported columns (first pass recommendation)**
สำหรับ `Teacher`:
- รหัสนักเรียน (`StudentId`)
- ชื่อ-นามสกุล (`Name`)
- ชื่อเล่น (`Nickname`)
- ห้อง (`NewRoom`)
- เลขที่ (`NewNo`)
- สถานะ (`Status`)

สำหรับ `ParentNetworkStaff`:
- ใช้ชุดเดียวกันได้ **เฉพาะถ้า field เหล่านี้ยังอยู่ใน low-sensitivity scope และ policy อนุญาต**
- ถ้ายังไม่ชัด ให้ deny export ก่อนในรอบแรก

ข้อสำคัญ:
- first pass **ไม่ export** เบอร์โทร, `LineUserId`, รูป, internal note, หรือข้อมูล guardian detail
- อย่าเปิดข้อมูลเพิ่มในไฟล์มากกว่าที่หน้า list ใช้อยู่ หากยังไม่มี sign-off ชัดเจน

### **D: Export mode**
first pass แนะนำ 1 mode ก่อน:
- export รายการตาม search/filter ปัจจุบันของหน้า list

ถ้า filter ยังมีแค่ `search` ก็ export ตาม `search` ก่อน

### **E: Out of scope in first pass**
- export student detail เต็มทุก field
- export guardian detail เต็ม
- export หลาย worksheet
- export พร้อม styling/branding หนัก ๆ
- export รูปแนบ
- async background export job
- huge dataset optimization ระดับ advanced

---

## 🔒 6. Authorization and PDPA Rules

### **A: Minimum recommended rule**
- `Teacher`: allow export student list Excel
- `ParentNetworkStaff`: default deny **จนกว่าจะชัดว่า allowed columns ชุดนี้ไม่เกินสิทธิ์**

นี่คือคำแนะนำแบบไม่อ้อม:
- ถ้ายังลังเลเรื่อง PDPA อย่าพยายามใจดีด้วยการเปิด export ให้ทุก role
- export ทำให้ข้อมูลเคลื่อนออกจากระบบได้ง่ายกว่า on-screen viewing มาก จึงควรเข้มกว่า list view ได้ด้วยซ้ำ

### **B: Error behavior**
- `401` เมื่อไม่มี token / token ไม่ valid
- `403` เมื่อ role ไม่มีสิทธิ์ export
- `400` เมื่อ query/filter ไม่ถูกต้อง
- `200` พร้อม file download เมื่อสำเร็จ

### **C: Audit concern (follow-up)**
รอบแรกอาจยังไม่ต้องมี audit log เต็มรูปแบบ แต่ควรเตรียมเผื่อไว้ว่าในอนาคตอาจต้อง log:
- ใคร export
- export เมื่อไร
- export ภายใต้ filter อะไร

---

## ⚙️ 7. Proposed Backend Design

### **A: New endpoint**
แนะนำ endpoint ใหม่ เช่น:
- `GET /api/backoffice/students/export.xlsx`

query parameters ควรรองรับอย่างน้อย:
- `search`
- future-friendly filter เช่น `room`, `status`

### **B: New service layer**
ควรเพิ่ม service แยก เช่น:
- `IBackofficeStudentExportService`
- `BackofficeStudentExportService`

หน้าที่:
- รับ current user role/context
- query dataset ที่ตรงกับ filter
- map rows ตาม export contract ของ role นั้น
- generate Excel bytes
- คืน filename + content type + stream/bytes

### **C: Shared query contract with list page**
เพื่อไม่ให้ list กับ export คนละโลก ควรมี query model กลาง เช่น:
- `BackofficeStudentListQuery`

ใช้ร่วมกันทั้ง:
- `/api/backoffice/students`
- `/api/backoffice/students/export.xlsx`

ข้อแนะนำ:
- ถ้ายังไม่ refactor list endpoint ตอนนี้ อย่างน้อย export endpoint ต้องรับ query แบบเดียวกับ UI state ปัจจุบัน
- รอบถัดไปค่อยขยับ list ไป server-side filtering ให้เต็มขึ้น

### **D: Excel library choice**
ต้องเพิ่ม package สำหรับ `.xlsx`

ตัวเลือกที่ practical:
- ClosedXML
- EPPlus (ต้องระวัง license)

ข้อแนะนำ:
- ใช้ `ClosedXML` จะตรงไปตรงมาและลด friction เรื่อง license

### **E: Filename strategy**
ชื่อไฟล์ควรสื่อความหมาย เช่น:
- `students-2026-04-06.xlsx`
- ถ้ามี search/filter อาจเพิ่ม suffix แบบปลอดภัย เช่น `students-search-somchai-2026-04-06.xlsx`

อย่าเอา raw query ยาว ๆ ไปแปะชื่อไฟล์มั่ว ๆ

---

## 🖥️ 8. Proposed Frontend Changes

### **A: Add export action on student list page**
ที่ `BackofficeStudentListView.vue` ควรเพิ่ม:
- ปุ่ม `Export Excel`
- loading state ตอนกำลัง export
- disabled state ถ้าผู้ใช้ไม่มีสิทธิ์

### **B: Frontend must call download endpoint, not build workbook itself**
service ที่น่าจะเพิ่ม:
- `exportStudentsExcel(token, query)`

หน้าที่ของ frontend:
- ส่ง query ปัจจุบันไป backend
- รับ blob
- trigger browser download

### **C: Permission-aware UI**
- ถ้า role ไม่มีสิทธิ์ export ต้องไม่แสดงปุ่ม หรือแสดง disabled พร้อมข้อความอธิบาย
- ห้ามพึ่งแค่ซ่อนปุ่ม ฝั่ง backend ต้องกันจริงด้วย

---

## 🗂️ 9. Target Files

### **Backend**
- `CsrApi/CsrApi.csproj`
- `CsrApi/BackofficeEndpoints.cs`
- service/export files ใหม่ใน `CsrApi/Services/`
- optional: DTO/query files ใหม่ใน `CsrApi/Models/` หรือโฟลเดอร์ที่เหมาะสม
- repository/query layer ถ้าต้องรองรับ filter server-side เพิ่ม

### **Frontend**
- `csr-frontend/src/views/backoffice/StudentListView.vue`
- `csr-frontend/src/services/backofficeApi.js`
- optional composable/helper ถ้าจะไม่ยัด download logic ไว้ใน view

### **Docs**
- `README.md` (เมื่อเริ่ม implement จริง)

---

## ✅ 10. Implementation Checklist

### **Backend contract / policy**
- [x] ตกลงให้ชัดว่า role ไหน export ได้บ้าง
- [x] ล็อก field list สำหรับ export first pass
- [x] เพิ่ม endpoint `/api/backoffice/students/export.xlsx`
- [x] แยก `401`, `403`, `400` ให้ชัด
- [x] ยืนยันว่า export ไม่คืนข้อมูลเกินสิทธิ์ของ role

### **Backend export generation**
- [x] เพิ่ม Excel package ที่เหมาะสม
- [x] เพิ่ม export service
- [x] map dataset เป็น row model ที่ทดสอบได้ง่าย
- [x] สร้าง workbook/worksheet แบบง่ายสำหรับ first pass
- [x] คืน filename + MIME type ถูกต้อง

### **Filter/query alignment**
- [x] กำหนด query contract ร่วมกับหน้า list
- [x] รองรับ `search` อย่างน้อยในรอบแรก
- [x] ยืนยันว่า export dataset ตรงกับ intent ของ user บนหน้า list

### **Frontend**
- [x] เพิ่มปุ่ม `Export Excel`
- [x] เพิ่ม download action ใน `backofficeApi.js`
- [x] จัดการ loading/error state
- [x] ซ่อนหรือ disable action ตามสิทธิ์

### **Verification**
- [ ] teacher export สำเร็จและเปิดไฟล์ได้จริง
- [ ] user ที่ไม่มีสิทธิ์ export ได้ `403`
- [ ] ข้อมูลใน Excel ตรงกับ dataset ที่ตั้งใจ export
- [ ] filename และ content type ถูกต้อง
- [ ] ไม่มี field อ่อนไหวหลุดไปในไฟล์โดยไม่ได้ตั้งใจ

### **Documentation**
- [ ] อัปเดต `README.md` วิธีใช้งาน export
- [ ] ระบุ role/policy ของการ export ให้ชัด

---

## 🧪 11. Acceptance Criteria
- ที่หน้า `backoffice/students` มี action สำหรับ export Excel
- backend มี endpoint export ที่ยืนยันสิทธิ์ก่อนเสมอ
- ไฟล์ `.xlsx` ที่ดาวน์โหลดเปิดได้จริงในโปรแกรม spreadsheet ทั่วไป
- ข้อมูลในไฟล์ตรงกับ scope ที่กำหนดสำหรับ first pass
- ถ้ามี search/filter ที่รองรับในรอบนี้ ไฟล์ต้องสะท้อน query นั้น
- role ที่ไม่มีสิทธิ์ export ต้องไม่สามารถดึงไฟล์ได้
- ไม่มีข้อมูลอ่อนไหวเกิน scope หลุดไปใน Excel

---

## 🚫 12. Out of Scope for First Pass
- export full student profile ทุก field
- export guardian detail เต็มรูปแบบ
- export document เป็น Excel
- export photo/file attachments
- async queued export job
- audit log เต็มรูปแบบ
- multi-sheet workbook
- styling/branding/report formatting ระดับสูง
- pivot/report summary ซับซ้อน

---

## ⚠️ 13. Risks / Notes
- ถ้าปล่อยให้ frontend export จาก array ใน browser จะ drift จาก backend policy ง่าย
- ถ้าเปิด export ให้ role ที่ policy ยังไม่ชัด จะเกิด PDPA risk ทันที
- ถ้า list filtering ยังอยู่ฝั่ง client ทั้งหมดนานเกินไป export กับสิ่งที่ผู้ใช้เห็นอาจไม่ตรงกันในอนาคต
- ถ้าใส่ field เยอะเกินในรอบแรก งานจะชนเรื่อง business sign-off ทันที
- ถ้าเลือก library ที่ติด license constraint ไม่เหมาะ จะสร้างปัญหาภายหลังโดยไม่จำเป็น

---

## 🧭 14. Recommended Delivery Order
1. ล็อก role policy ของ export ให้ชัด
2. ล็อก exported columns first pass
3. เพิ่ม backend export endpoint + service
4. เพิ่ม frontend export button + download flow
5. ทดสอบ teacher happy path + unauthorized path
6. ค่อยขยับไป filter alignment และ role expansion เพิ่มเติม

---

## 🧠 15. Honest Recommendation
ถ้าจะทำงานนี้ให้คุ้ม:
- เริ่มจาก **Teacher only** ก่อน
- export แค่ **student list columns ที่ไม่เสี่ยงเกินจำเป็น**
- ให้ backend generate Excel
- อย่ายัด guardian detail, phone, note, และข้อมูลอ่อนไหวอื่นเข้ารอบแรก
- ถ้าภายหลังอยาก export ตาม filter หลายแบบ ค่อยย้าย list query ไป server-side ให้ครบ

แบบนี้จะได้ feature ที่ใช้งานจริงได้เร็ว และไม่เปิดช่องข้อมูลหลุดแบบโง่ ๆ
