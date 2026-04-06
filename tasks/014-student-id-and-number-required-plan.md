# ✅ Phase 14: Require Student ID and Student Number in Registration Flow
**Status:** Implemented
**Goal:** ปรับกติกาการลงทะเบียนให้ `รหัสนักเรียน (Student ID)` และ `เลขที่` เป็น field บังคับกรอกทั้งในระดับ frontend และ backend โดยยังคงให้ database schema เป็น nullable ชั่วคราวเพื่อไม่กระทบข้อมูลเดิม

---

## 🎯 1. Problem Statement
ก่อนหน้านี้ตกลงกันว่า `Student ID` และ `เลขที่` ไม่บังคับกรอก จึงทำให้ implementation ปัจจุบันยังรองรับค่าว่างอยู่หลายจุด:

- หน้า `Register.vue` แสดง placeholder เชิง optional
- frontend ส่ง `null` ได้ถ้าผู้ใช้ไม่กรอก
- backend ยังไม่มี business validation เพื่อบล็อก request ที่ส่งค่าว่าง
- database schema ยังเป็น nullable
- `StudentId` ยังไม่ได้ถูก enforce เรื่องความไม่ซ้ำกันในระดับฐานข้อมูล

สถานะตอนนี้เปลี่ยนแล้ว: **ทั้งสอง field ต้องบังคับกรอก** และ **`StudentId` ต้องไม่ซ้ำกัน**

สิ่งที่ต้องทำให้ชัดคือ แม้ฐานข้อมูลจะยัง nullable ได้ชั่วคราว แต่ **registration flow ต้องไม่ยอมรับค่าว่างอีกต่อไป** และในระยะถัดไปต้อง enforce ความ unique ของ `StudentId` ให้ตรงกับ business rule

---

## 🧩 2. Scope of Work

### **A: Frontend UI update**
ปรับหน้าแบบฟอร์มให้สื่อชัดว่า 2 field นี้เป็น required:

- `รหัสประจำตัว (Student ID)`
- `เลขที่`

สิ่งที่ต้องเปลี่ยน:
- label ต้องมี `*` สีแดง
- ลบข้อความ placeholder ที่สื่อว่าเว้นว่างได้
- field invalid ต้องมี visual state และข้อความ error ใต้ field
- required marker ต้องใช้รูปแบบเดียวกันกับ field required อื่นในหน้า
- ถ้า implementation ฝั่ง `tasks/008` มี helper/component กลางสำหรับ label หรือ error state แล้ว ควร reuse แทนการ hardcode ซ้ำ

### **A.1: Date of birth input UX**
ปรับ field `วันเกิด` ให้ใช้งานได้สอดคล้องกับรูปแบบที่ผู้ใช้ไทยคุ้นเคย

สิ่งที่ต้องเปลี่ยน:
- format ที่ผู้ใช้เห็นและกรอกต้องเป็น `dd/MM/yyyy`
- ปุ่มหรือไอคอน calendar ต้องเปิดตัวเลือกวันที่ได้จริง
- ห้ามพึ่งพฤติกรรมของ native `input type="date"` อย่างเดียว เพราะ format และ calendar UI ไม่สม่ำเสมอข้าม browser/OS
- แนวทางที่แนะนำคือใช้ `@vuepic/vue-datepicker` แทน native date input
- UI ต้องแสดงค่าเป็น `dd/MM/yyyy`
- ค่าที่ส่งเข้า API ควรถูก normalize เป็น `yyyy-MM-dd` เพื่อลดความเสี่ยงเรื่องวัน/เดือนสลับกัน
- ต้องกำหนดให้ชัดว่า format ที่ใช้ใน UI กับ format ที่ส่งเข้า API จะแปลงกันอย่างไร เพื่อไม่ให้ข้อมูลวันเกิดเพี้ยน

### **B: Client-side validation**
เพิ่ม validation ฝั่ง frontend เพื่อ block ก่อน submit

Validation rule ขั้นต่ำ:
- `studentId` ห้ามว่างหลัง trim
- `newNo` ห้ามเป็น `null`
- `newNo` ต้องเป็นจำนวนเต็มบวก
- `dob` ถ้ามีการบังคับ format ฝั่ง UI ต้อง parse ได้ถูกต้องและไม่เกิดวัน/เดือนสลับกัน

ข้อกำหนดเชิงโครงสร้าง:
- ไม่ยัด logic validation ก้อนใหญ่ใน `Register.vue`
- วาง validation state / helper ใน `useRegistrationForm.js` หรือ helper ที่แยกทดสอบได้ง่าย
- ห้ามพึ่ง browser native validation popup เป็นหลัก
- error message ต้องแสดงใกล้ field และใช้ภาษาที่ผู้ใช้ทั่วไปเข้าใจได้

### **C: Server-side validation**
เพิ่ม validation ฝั่ง backend เพื่อให้กติกา enforced จริงในระบบ

Validation rule ขั้นต่ำ:
- `request.Student.StudentId` ต้องไม่เป็น `null` / empty / whitespace
- `request.Student.NewNo` ต้องมีค่า
- `request.Student.NewNo` ต้องมากกว่า `0`
- `request.Student.StudentId` ต้องไม่ซ้ำกับนักเรียนคนอื่น

Expected behavior:
- ถ้า invalid ต้องคืน `400 Bad Request`
- message ควรชัดและตรงกับ field ที่ผิด

### **D: Database compatibility**
สำหรับรอบนี้ **ยังไม่เปลี่ยน schema DB**

แนวทาง:
- model ยังเป็น nullable ได้
- column ใน SQLite ยังไม่ต้องเปลี่ยนเป็น `NOT NULL`
- ใช้ application-layer validation บังคับกติกาไปก่อน

เหตุผล:
- ลดความเสี่ยงกับข้อมูลเก่าที่อาจยังว่าง
- ทำ migration แยกได้ในรอบถัดไปถ้าต้องการคุม schema ให้เข้มขึ้น

### **E: StudentId uniqueness and migration concern**
เนื่องจาก business rule ใหม่กำหนดให้ `StudentId` ต้องไม่ซ้ำกัน จึงต้องออกแบบเผื่อการคุมทั้งในระดับ application และ database

สิ่งที่ต้องพิจารณา:
- ในรอบนี้ควรมีอย่างน้อย server-side validation เพื่อตรวจ `StudentId` ซ้ำก่อนบันทึก
- ในรอบถัดไปควรเพิ่ม database constraint หรือ unique index เพื่อป้องกันข้อมูลซ้ำในกรณี concurrent requests
- ก่อนเพิ่ม unique constraint ต้องตรวจและจัดการข้อมูลเดิมที่อาจมี `StudentId` ซ้ำอยู่แล้ว
- README ต้องแยกให้ชัดว่าอะไรคือ current schema และอะไรคือ target business rule ถ้ายังไม่ได้ทำ migration ในรอบนี้

---

## ⚙️ 3. Suggested Implementation Plan

### **Target files**
- `csr-frontend/src/views/Register.vue`
- `csr-frontend/src/composables/useRegistrationForm.js`
- `csr-frontend/src/services/registrationApi.js`
- `CsrApi/Services/RegistrationService.cs`
- `README.md`
- `csr-frontend/package.json` (ถ้าต้องเพิ่ม date picker library)
- `csr-frontend/src/main.js` หรือ global stylesheet/component import point (ถ้า library ต้องมี global CSS)

### **Implementation outline**

#### **Frontend**
- เพิ่ม required marker ให้ `Student ID` และ `เลขที่`
- bind error state สำหรับทั้งสอง field
- block submit ถ้ายัง validation ไม่ผ่าน
- ถ้าเป็นไปได้ให้ focus หรือ scroll ไป field แรกที่ผิดโดยไม่ทำโค้ดยุ่งเกินจำเป็น
- รักษา visual pattern ของ label, error text, และ invalid state ให้สอดคล้องกับแนวทางใน `tasks/008`
- เปลี่ยน field `วันเกิด` จาก native `input type="date"` ไปใช้ `@vuepic/vue-datepicker`
- กำหนดให้ผู้ใช้เห็นค่า `วันเกิด` เป็น `dd/MM/yyyy`
- ทำให้การกด input หรือปุ่ม calendar เปิดตัวเลือกวันที่ได้จริง
- กำหนดให้ชัดว่าค่า `dob` ใน form state, UI display, และ payload ใช้ format อะไรบ้าง โดย normalize ตอนส่ง API เป็น `yyyy-MM-dd`

#### **Composable / validation helper**
- เพิ่ม `errors` state สำหรับ student fields
- แยกฟังก์ชันเล็ก ๆ เช่น:
  - `validateStudentSection()`
  - `validateRegistrationForm()`
- ใช้ guard clauses แทน nested `if` ลึก
- ตั้งชื่อ field error ให้ชัด เช่น `errors.student.studentId` และ `errors.student.newNo`
- แยก helper สำหรับแปลง `dob` ระหว่าง `@vuepic/vue-datepicker`, UI format `dd/MM/yyyy`, และ payload format `yyyy-MM-dd` เพื่อไม่ให้ logic ปะปนใน `Register.vue`

#### **Backend service**
- เพิ่ม method validation ขนาดเล็กก่อนบันทึกข้อมูล
- ห้ามปล่อย request invalid หลุดไปถึง repository
- รักษาโค้ดให้ testable และแยก concern ชัดเจน
- เพิ่มการตรวจ `StudentId` ซ้ำในระดับ service ก่อน upsert

#### **Database follow-up**
- เตรียมงานแยกสำหรับตรวจข้อมูลซ้ำของ `StudentId`
- ถ้าข้อมูลสะอาดแล้ว ให้เพิ่ม unique constraint หรือ unique index ใน schema รอบถัดไป
- ระวังกรณี migration ล้มเพราะมีข้อมูลเดิมซ้ำกันอยู่แล้ว

#### **README**
อัปเดตเอกสารให้ตรงกับ behavior ใหม่ว่า:
- `Student ID` และ `เลขที่` เป็น required ใน registration flow
- database ยัง nullable ชั่วคราวเพื่อรองรับข้อมูลเดิม
- `StudentId` เป็น target business rule ที่ต้อง unique และควรมี migration ตามมาภายหลังถ้ายังไม่ enforce ใน schema รอบนี้

---

## ✅ 4. Implementation Checklist

### **Frontend UI**
- [x] เพิ่ม required marker (`*`) ที่ label ของ `Student ID` และ `เลขที่` — ใช้ `FieldLabel :required="true"`
- [x] ลบ placeholder ที่สื่อว่า field ทั้งสองยังเว้นว่างได้
- [x] แสดง error message ใต้ field เมื่อ validation ไม่ผ่าน
- [x] เพิ่ม invalid visual state ให้ input ของ `Student ID` และ `เลขที่` — ใช้ `inputClass()` เปลี่ยน border เป็นแดง
- [x] ตรวจให้แน่ใจว่า required marker และ error style สอดคล้องกับ pattern จาก `tasks/008` — reuse `FieldLabel` component
- [x] ปรับ field `วันเกิด` ให้ผู้ใช้เห็น format เป็น `dd/MM/yyyy` — `:formats="{ input: 'dd/MM/yyyy' }"`
- [x] แก้ปัญหาให้ปุ่มหรือไอคอน calendar เปิด date picker ได้จริง — ใช้ `@vuepic/vue-datepicker`
- [x] ติดตั้งและใช้งาน `@vuepic/vue-datepicker` — v12.1.0
- [x] import CSS/asset ของ `@vuepic/vue-datepicker` ให้ครบตามที่ library ต้องใช้
- [x] จำกัด `เลขที่` ไม่เกิน 50 — `max="50"` บน input + `positiveInteger(..., 50)` validation

### **Frontend validation logic**
- [x] เพิ่ม `errors.student.studentId`
- [x] เพิ่ม `errors.student.newNo`
- [x] เพิ่ม validation ว่า `studentId` ต้องไม่ว่างหลัง trim — `required()` helper
- [x] เพิ่ม validation ว่า `newNo` ต้องไม่เป็น `null` — `positiveInteger()` helper
- [x] เพิ่ม validation ว่า `newNo` ต้องมากกว่า `0` — `positiveInteger()` helper
- [x] เพิ่ม validation ว่า `newNo` ต้องไม่เกิน 50 — `positiveInteger(value, label, 50)`
- [x] block submit ถ้า validation ไม่ผ่าน — `if (!validateForm()) return;`
- [ ] ถ้าเหมาะสม ให้ focus หรือ scroll ไป field แรกที่ผิด — **nice-to-have ไว้รอบถัดไป**
- [x] หลีกเลี่ยงการพึ่ง browser native validation popup เป็นหลัก — custom validation + `novalidate`
- [x] เพิ่ม helper สำหรับ parse/format `dob` — `parseDobToApi()`, `parseApiToDob()` ใน `validators.js`
- [x] ยืนยันว่าไม่มีปัญหาวัน/เดือนสลับกันตอนโหลดข้อมูลเดิมและตอน submit — model-type `yyyy-MM-dd` ทั้งสองทาง

### **Backend validation logic**
- [x] เพิ่ม method validation ใน `RegistrationService` — `ValidateStudentInfo()`
- [x] reject request ถ้า `request.Student.StudentId` ว่าง — `string.IsNullOrWhiteSpace` check
- [x] reject request ถ้า `request.Student.StudentId` ซ้ำกับนักเรียนคนอื่น — `GetStudentByStudentIdAsync()` + Id comparison
- [x] reject request ถ้า `request.Student.NewNo` ไม่มีค่า — `null` check
- [x] reject request ถ้า `request.Student.NewNo <= 0`
- [x] reject request ถ้า `request.Student.NewNo > 50`
- [x] คืน `400 Bad Request` พร้อมข้อความที่ชัดเจน — `AppError.BadRequest()` with Thai messages
- [x] กันไม่ให้ request invalid หลุดไปถึง repository — validation runs before repo calls

### **StudentId uniqueness / migration follow-up**
- [ ] ตรวจว่ามีข้อมูล `StudentId` ซ้ำในฐานข้อมูลปัจจุบันหรือไม่ — **รอบถัดไป**
- [ ] ถ้ามีข้อมูลซ้ำ ให้กำหนดแนวทาง cleanup ก่อนเพิ่ม unique constraint — **รอบถัดไป**
- [ ] วางแผน migration หรือ unique index สำหรับ `StudentId` ในรอบถัดไป — **รอบถัดไป**
- [x] ระบุในเอกสารให้ชัดว่า current schema กับ target rule ต่างกันตรงไหน — อัปเดตใน `README.md`

### **Compatibility / regression checks**
- [x] ยืนยันว่าไม่ได้เปลี่ยน DB schema หรือบังคับ `NOT NULL`
- [x] ยืนยันว่าข้อมูลเก่าที่ `studentId` หรือ `newNo` ยังอ่านได้
- [x] ยืนยันว่าหน้า edit/save บังคับให้กรอกสอง field นี้ก่อน submit — same `validateForm()` runs
- [x] ยืนยันว่า registration flow เดิมยังทำงานได้หลังเพิ่ม validation
- [x] ยืนยันว่า field `วันเกิด` แสดงผลถูกต้องตาม `dd/MM/yyyy` ทั้งตอน create และ edit
- [ ] ยืนยันว่า date picker ใช้งานได้จริงบน environment ที่ใช้งานจริง — **ต้องทดสอบบน LINE LIFF**
- [x] ยืนยันว่า backend ยังรับค่า `dob` ที่ normalize แล้วในรูปแบบ `yyyy-MM-dd` ได้ตามเดิม
- [x] ยืนยันว่า update ข้อมูลของนักเรียนคนเดิมไม่ถูกตีความผิดว่า `StudentId` ซ้ำกับตัวเอง — เปรียบเทียบ `existingByStudentId.Id != studentId`

### **Documentation**
- [x] อัปเดต `README.md` ให้สะท้อน business rule ใหม่
- [x] ระบุในเอกสารว่า DB ยัง nullable ชั่วคราว แม้ registration flow จะบังคับกรอกแล้ว

---

## 🔒 5. Constraints / Rules
- ห้ามเปลี่ยน DB schema ใน task นี้
- ห้ามพึ่ง browser native validation popup เป็นหลัก
- validation logic ต้องไม่กระจุกใน `Register.vue`
- หลีกเลี่ยง nested `if` ลึกเกิน 2 ชั้น
- helper/function ควรแยกให้ unit test ได้ง่าย
- งานนี้ต้อง align กับแนวทาง Required Field Indicator & Validation UX จาก `tasks/008` แต่จำกัด scope เฉพาะ `Student ID` และ `เลขที่`
- เรื่อง `วันเกิด` ใน task นี้ครอบคลุมเฉพาะ format/UX ของ date input และการแปลงค่าที่เกี่ยวข้อง ไม่ขยายเป็นงานจัดการปฏิทินไทยหรือปี พ.ศ.
- สำหรับ `วันเกิด` ให้ยึด approach หลักเป็น `@vuepic/vue-datepicker` + UI format `dd/MM/yyyy` + payload format `yyyy-MM-dd`

---

## 🧪 6. Acceptance Criteria
- หน้า form แสดงว่า `Student ID` และ `เลขที่` เป็น required อย่างชัดเจน
- ถ้าปล่อยว่างแล้ว submit ต้องไม่ผ่าน
- ถ้า `StudentId` ซ้ำกับนักเรียนคนอื่น ระบบต้อง reject อย่างชัดเจน
- ถ้า `เลขที่` เป็น `0`, ค่าติดลบ, หรือค่าที่ไม่ถูกต้อง ต้องไม่ผ่าน
- field ที่ผิดต้องมีข้อความ error ใต้ input อย่างชัดเจน
- input ที่ invalid ต้องมี visual state แยกจาก input ปกติ
- field `วันเกิด` ต้องใช้ `@vuepic/vue-datepicker` และแสดง/กรอกในรูปแบบ `dd/MM/yyyy` ที่ผู้ใช้ไทยเข้าใจง่าย
- ผู้ใช้ต้องสามารถเปิด calendar/date picker เพื่อเลือกวันเกิดได้จริง
- ค่าวันเกิดที่ส่งเข้า API ต้องถูก normalize เป็น `yyyy-MM-dd`
- ค่าวันเกิดที่แสดงใน UI กับค่าที่ส่งเข้า API ต้องไม่เกิดปัญหาวัน/เดือนสลับกัน
- backend `/api/register` ต้อง reject payload ที่ไม่มี `studentId` หรือ `newNo`
- response invalid ต้องเป็น `400 Bad Request`
- ข้อมูลเก่าที่ DB ยังเป็น `null` ต้องยังอ่านได้ตามปกติ
- แต่เมื่อผู้ใช้กลับมา edit/save ต้องกรอก field ให้ครบตามกติกาใหม่
- registration flow เดิมยังต้องทำงานได้หลังเพิ่ม validation นี้

---

## ⚠️ 7. Risks / Notes
- ถ้ามีข้อมูลเก่าที่ `studentId` หรือ `newNo` เป็นค่าว่าง หน้าแก้ไขข้อมูลจะเปิดขึ้นมาในสถานะที่ยังไม่พร้อม submit จนกว่าผู้ใช้จะกรอกให้ครบ
- ถ้าทำแค่ frontend โดยไม่เพิ่ม backend validation จะเป็น required ปลอม เพราะ caller อื่นยังยิง API ตรงได้
- ถ้า `StudentId` ต้อง unique จริง แต่ยังไม่ enforce ในระดับฐานข้อมูล ระบบยังมีช่องโหว่เรื่อง concurrent writes อยู่
- ถ้าจะเพิ่ม unique constraint ภายหลังโดยไม่ตรวจข้อมูลเดิมก่อน migration มีโอกาสพังทันทีถ้ามีข้อมูลซ้ำอยู่แล้ว
- ถ้าภายหลังต้อง enforce ระดับฐานข้อมูลจริง ควรทำ migration หลังจากเคลียร์ข้อมูลเก่าเรียบร้อยแล้ว

---

## 🧭 8. Nice-to-have (not required in this task)
- สรุป error box ด้านบนฟอร์ม
- touched state / realtime validation หลังเริ่มกรอก
- unit tests สำหรับ validation helpers และ service validation
- migration ภายหลังเพื่อเปลี่ยน `StudentId` / `NewNo` ให้เข้มขึ้นในระดับ schema
- unit tests สำหรับกรณี `StudentId` ซ้ำและกรณี update record เดิม
