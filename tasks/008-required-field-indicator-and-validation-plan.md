# ✅ Phase 6: Required Field Indicator & Form Validation UX
**Status:** Planned
**Goal:** ปรับปรุงฟอร์มลงทะเบียน/แก้ไขข้อมูลให้ใช้งานได้ชัดเจนขึ้น โดยช่องที่บังคับกรอกต้องมีเครื่องหมาย `*` สีแดง และระบบต้องมี validation ที่ชัดเจนทั้งก่อน submit และระหว่างกรอกข้อมูล

---

## 🎯 1. Problem Statement
ปัจจุบันฟอร์มมี `required` บางช่องในระดับ HTML อยู่แล้ว แต่ UX ยังไม่ชัดพอสำหรับผู้ใช้จริง:
- ผู้ใช้ไม่รู้ทันทีว่าช่องไหนบังคับกรอก
- validation message ยังไม่เป็นระบบและไม่ได้แสดงใกล้ field อย่างสม่ำเสมอ
- ถ้ากด submit แล้วข้อมูลไม่ครบ ประสบการณ์ใช้งานยังไม่ชัดว่าต้องแก้ตรงไหน
- logic validation เสี่ยงไปกระจุกอยู่ใน `Register.vue` ถ้าไม่ออกแบบให้ดี

---

## 🧩 2. Scope of Work

### **A: Required field indicator**
ทุกช่องที่เป็น required ต้องแสดง label พร้อม `*` สีแดงอย่างสม่ำเสมอ

ตัวอย่าง field ที่ควรมีเครื่องหมายบังคับกรอก:
- ชื่อ-นามสกุลนักเรียน
- ชื่อ-นามสกุลผู้ปกครอง
- ความสัมพันธ์กับนักเรียน
- เบอร์โทรศัพท์ผู้ปกครอง
- field อื่นที่ business rule ระบุว่า required

ข้อกำหนด UX:
- ใช้รูปแบบเดียวกันทั้งหน้า
- `*` ต้องเห็นชัด แต่ไม่รบกวนสายตาเกินไป
- ไม่ hardcode style ซ้ำหลายจุด ถ้าเป็นไปได้ควรมี helper/component กลาง

### **B: Client-side validation**
ต้องมี validation ระดับ frontend ก่อน submit

Validation ขั้นต่ำที่ควรมี:
- required fields ต้องไม่ว่าง
- เบอร์โทรควรมีรูปแบบสมเหตุสมผล
- email ถ้ากรอก ต้องเป็นรูปแบบที่ถูกต้อง
- number fields เช่น เลขที่ ต้องไม่ติดลบหรือผิดประเภท
- รูปที่เลือก ถ้ามี validation เฉพาะ field เพิ่มเติม ต้องแสดงผลชัดเจน

### **C: Validation error presentation**
เมื่อ validation ไม่ผ่าน ต้องแสดงผลแบบใช้งานได้จริง

สิ่งที่ควรมี:
- error message ใต้ field ที่ผิด
- input เปลี่ยนสี/ขอบเพื่อบอกสถานะ error
- submit ต้องไม่วิ่งต่อถ้ายัง invalid
- หลัง submit ไม่ผ่าน ควร focus หรือ scroll ไปยัง field แรกที่ผิดถ้าทำได้โดยไม่ทำโค้ดยุ่งเกินจำเป็น

### **D: Architecture constraint**
- ห้ามยัด validation logic หนักกลับเข้า `Register.vue`
- ควรวาง state/logic validation ไว้ใน composable เช่น `useRegistrationForm.js` หรือแยก helper ที่ unit test ได้ง่าย
- ลด duplication ระหว่าง student section และ guardian section

---

## ⚙️ 3. Suggested Implementation Plan

### **Target files**
- `csr-frontend/src/views/Register.vue`
- `csr-frontend/src/composables/useRegistrationForm.js`
- ถ้าจำเป็นค่อยเพิ่ม component/helper ใหม่ เช่น
  - reusable label component
  - validation helper

### **Suggested design**
- เพิ่มโครงสร้าง `errors` state แยกตาม field เช่น
  - `errors.student.name`
  - `errors.guardian.phone`
- สร้าง function สำหรับ validation โดยไม่ให้ยาวเกินไป เช่น
  - `validateStudentForm()`
  - `validateGuardianForm()`
  - `validateRegistrationForm()`
- แยก helper สำหรับตรวจ required, phone, email เพื่อให้ทดสอบง่ายและไม่ซ้ำโค้ด
- หน้า `Register.vue` ควรมีหน้าที่ bind UI กับ error state มากกว่าเขียน logic validation เองทั้งหมด

---

## 🔒 4. UX / Quality Rules
- required marker ต้องสม่ำเสมอทั้งหน้า
- error message ต้องใช้ภาษาที่ผู้ใช้ทั่วไปเข้าใจได้
- ห้ามพึ่ง browser native validation popup เป็นหลัก เพราะควบคุม UX ได้ไม่ดี
- validation ต้องไม่ทำให้หน้า form ซับซ้อนจน maintain ยาก
- หลีกเลี่ยง nested `if` ลึกเกินจำเป็น ใช้ guard clauses และ helper functions แทน

---

## 🧪 5. Acceptance Criteria
- ทุก required field แสดง `*` สีแดงที่ label
- ถ้าปล่อย required field ว่างแล้วกด submit ระบบต้อง block การ submit
- field ที่ผิดต้องมีข้อความ error ใต้ input อย่างชัดเจน
- input ที่ invalid ต้องมี visual state แยกจาก input ปกติ
- ถ้า email ไม่ว่างแต่ format ไม่ถูกต้อง ต้อง submit ไม่ผ่าน
- ถ้าเบอร์โทรไม่ผ่าน rule ที่กำหนด ต้อง submit ไม่ผ่าน
- โค้ด validation ไม่กระจุกเป็นก้อนใหญ่ใน `Register.vue`
- registration flow เดิมยังทำงานได้หลังเพิ่ม validation

---

## 🧭 6. Nice-to-have (not required in this task)
- validate แบบ realtime หลัง field ถูกแตะแล้ว (`touched` state)
- summary error box ด้านบนฟอร์ม
- reusable `FormField` / `FieldLabel` component สำหรับลด duplication
- unit tests สำหรับ validation helpers
