# 📄 Phase 7: Student & Family Introduction Document View / Export
**Status:** Planned
**Goal:** สร้างฟีเจอร์สำหรับแสดงผลและ export เอกสารแนะนำตัวนักเรียนและครอบครัวตามแบบใน `design/306633.jpg` โดยรองรับการจัดหน้าเอกสารที่พร้อมพิมพ์/บันทึกเป็น PDF และไม่ทำให้ logic กระจุกเป็นก้อนใหญ่ในหน้าเดียว

---

## 🎯 1. Problem Statement
แบบเอกสารใน `design/306633.jpg` ไม่ใช่แค่ card/profile ธรรมดา แต่เป็นเอกสารลักษณะฟอร์มแนะนำตัวที่มี layout ตายตัวและมีข้อมูลหลายส่วน เช่น:
- รูปภาพ 3 ช่องด้านบน
- ข้อมูลนักเรียน
- ข้อมูลผู้ปกครองคนที่ 1
- ข้อมูลผู้ปกครองคนที่ 2
- ช่อง `Line ID` หลายตำแหน่ง

ปัญหาตรง ๆ คือ **data model ปัจจุบันยังไม่พร้อม** สำหรับเอกสารนี้เต็มรูปแบบ และตอนนี้ได้ข้อสรุปแล้วว่าต้องปรับ model จริง ไม่ใช่ workaround ชั่วคราว:
- schema ต้องรองรับ guardian 2 คนต่อ 1 นักเรียน
- `Line ID` ในเอกสารหมายถึง `LineUserId` ของบัญชี LINE ที่ใช้ในแอป
- ต้องเพิ่ม field ชื่อ/นามสกุลแยกจริงใน schema แทนการพยายาม split จาก full name ตอน render
- document payload ต้อง map จาก model ใหม่โดยตรง ไม่ใช้ derived parsing จากชื่อเต็ม

ถ้าข้ามจุดนี้แล้วไปทำ export เลย จะได้เอกสารที่หน้าตาคล้ายแบบ แต่ข้อมูลจริงไม่ครบตาม requirement

---

## 🧩 2. Scope of Work

### **A: Canonical document data model**
ต้องนิยาม document payload กลางสำหรับงานเอกสารนี้ก่อน โดยแยกจาก registration form model ให้ชัด

ข้อมูลขั้นต่ำที่ควรรองรับ:
- student photo
- guardian photo 1
- guardian photo 2
- ชื่อนักเรียน
- นามสกุลนักเรียน
- ชื่อเล่น
- เบอร์ติดต่อ
- `LineUserId` ของนักเรียนตามที่ระบบเก็บจริง
- ข้อมูลผู้ปกครองคนที่ 1
- ข้อมูลผู้ปกครองคนที่ 2
- อาชีพ
- ความสัมพันธ์

ข้อสรุปสำหรับ phase นี้:
- field ที่แบบเอกสารต้องใช้และยังไม่มีในระบบ ให้เพิ่ม schema จริง
- ห้ามใช้การ split ชื่อเต็มแบบเดา ๆ เป็นคำตอบหลัก
- document payload ต้องสะท้อน guardian 2 คนอย่าง explicit ไม่ใช่ยัด guardian คนที่ 2 เป็น optional blob แบบกำกวม

### **B: Document view page**
ต้องมีหน้าแสดงเอกสารแบบ A4/print-friendly ที่ render จากข้อมูลจริง ไม่ใช่ hardcode mock layout

คุณสมบัติที่ควรมี:
- สัดส่วนและ spacing ใกล้เคียงแบบในภาพ
- รองรับข้อมูลหายบางช่องโดย layout ไม่พัง
- รองรับรูปภาพจาก protected endpoints ที่มีสิทธิ์เข้าถึงถูกต้อง
- แยก component ย่อยเพื่อไม่ให้ไฟล์ยาวเกินไป

### **C: Export / print flow**
สำหรับ phase แรกควรใช้แนวทาง **HTML print-first** ก่อน

แนวทางที่แนะนำ:
- render หน้า document เป็น HTML/CSS สำหรับพิมพ์
- ใช้ `window.print()` หรือ print dialog ของ browser เพื่อให้ผู้ใช้ export เป็น PDF
- ออกแบบ print stylesheet สำหรับ A4 โดยเฉพาะ

เหตุผล:
- เร็วกว่าและ maintain ง่ายกว่า backend PDF generation
- ใช้ layout เดียวกันสำหรับ preview และ export
- ลด dependency หนักฝั่ง .NET หรือ Node โดยไม่จำเป็น

ถ้าภายหลังต้องการ PDF ที่ pixel-perfect หรือ batch export ค่อยพิจารณา backend PDF generation เป็น phase ใหม่

### **D: Data retrieval & authorization**
เอกสารนี้มีข้อมูลส่วนบุคคล จึงต้องยืนยันสิทธิ์การเข้าถึงชัดเจน

ข้อกำหนด:
- ใช้ token/session เดิมแบบเดียวกับ `/api/me`
- ห้ามเปิด endpoint ที่เดา `studentId` แล้วอ่านเอกสารใครก็ได้
- ถ้าจะมี document endpoint ใหม่ ต้องคืนเฉพาะข้อมูลของผู้ใช้ที่มีสิทธิ์
- รูปภาพยังคงใช้ protected access ไม่เปิด public static serving

---

## ⚙️ 3. Suggested Implementation Plan

### **Phase A: Lock business/data contract (resolved decisions)**
ข้อสรุปที่ใช้เป็นฐาน implement ต่อจากนี้:
- guardian ต้องรองรับ 2 คนใน schema และใน registration flow
- `Line ID` ในเอกสารหมายถึง `LineUserId` ที่ใช้ในแอป LINE
- ชื่อและนามสกุลต้องแยก field จริงใน schema ของ student และ guardian
- document payload ต้อง map จาก model ใหม่โดยตรง ไม่ใช้ derived parsing จากชื่อเต็ม

สิ่งที่ยังต้องสรุปเพิ่มเติมเชิง business แต่ไม่ควรขวางการออกแบบ model:
- รูปช่องที่ 3 ด้านบนจะเป็นรูป guardian คนที่ 2 แน่นอน หรือเป็น slot generic ตามประเภทบุคคล
- ในกรณี guardian คนที่ 2 ไม่มีข้อมูล จะปล่อยว่างทั้ง section หรือซ่อน section บางส่วนอย่างไรใน print layout

### **Phase B: Backend DTO and endpoint**
ไฟล์ที่คาดว่าจะเกี่ยวข้อง:
- `CsrApi/Models/Student.cs`
- `CsrApi/Models/Guardian.cs`
- `CsrApi/Models/RegistrationRequest.cs`
- `CsrApi/Models/ProfileResponse.cs`
- `CsrApi/Services/RegistrationService.cs`
- `CsrApi/Repositories/StudentRepository.cs`
- `CsrApi/Program.cs`
- schema/bootstrap/seed code ที่เกี่ยวข้อง

แนวทาง:
- ปรับ `Student` ให้มีชื่อ/นามสกุลแยก เช่น encrypted first name / last name แทนเก็บชื่อรวมอย่างเดียว
- ปรับ `Guardian` ให้รองรับ 2 records ต่อ student โดยมี field สำหรับลำดับหรือ role ที่แยก guardian คนที่ 1/2 ได้ชัด
- เพิ่ม DTO ใหม่ เช่น `IntroductionDocumentResponse`
- ปรับ `RegistrationRequest` ให้รับ guardian 2 คนอย่าง explicit
- ปรับ `ProfileResponse` หรือเพิ่ม response ใหม่ที่สะท้อน model ใหม่นี้
- แยก mapping logic ออกจาก endpoint handler
- เพิ่ม endpoint เช่น `GET /api/me/introduction-document`
- endpoint นี้ควรคืนเฉพาะข้อมูลที่จำเป็นต่อการ render เอกสาร

### **Phase C: Frontend document module**
ไฟล์ที่คาดว่าจะเกี่ยวข้อง:
- `csr-frontend/src/composables/useRegistrationForm.js`
- `csr-frontend/src/views/` เพิ่มหน้าใหม่สำหรับ document preview
- `csr-frontend/src/services/registrationApi.js` หรือแยก service ใหม่สำหรับ document
- `csr-frontend/src/components/` สำหรับ section ย่อยของเอกสาร

แนวทาง component ที่แนะนำ:
- `IntroductionDocumentView.vue`
- `DocumentPhotoRow.vue`
- `StudentDocumentSection.vue`
- `GuardianDocumentSection.vue`

ข้อสำคัญ:
- อย่ายัด layout + data fetching + export logic ไว้ใน component เดียว
- แยก composable สำหรับโหลดข้อมูลเอกสาร เช่น `useIntroductionDocument.js`
- component แต่ละตัวควร test ได้ง่ายและรับ props ชัดเจน

### **Phase D: Print stylesheet and export UX**
สิ่งที่ต้องมี:
- print CSS สำหรับ A4
- ซ่อนปุ่ม/toolbar ตอน print
- คุม page margin, image box ratio, line height และ spacing
- ปุ่ม `พิมพ์ / บันทึกเป็น PDF`
- loading/error state ตอนดึงข้อมูลเอกสาร

ข้อควรระวัง:
- อย่าพึ่ง lib แปลง HTML เป็น canvas/PDF ฝั่ง client เร็วเกินไป ถ้ายังไม่ได้ลอง print CSS ให้ดีพอ เพราะจะเพิ่มความเปราะและคุณภาพตัวอักษรอาจแย่

---

## 🏗️ 4. Data Model Gap That Must Be Resolved
นี่คือจุดที่ตกลงแล้วว่าต้องแก้จริงใน model:

### **A: Guardian multiplicity mismatch**
แบบร่างมีผู้ปกครอง 2 คน และ phase นี้ต้องปรับ schema ให้รองรับ 2 คนจริง

ข้อกำหนดที่ควรใช้:
- 1 student : many guardians ในระดับ schema แต่ business rule ของฟอร์ม phase นี้ต้องรองรับอย่างน้อย 2 คน
- ต้องมี field ที่บอกลำดับหรือประเภทของ guardian เพื่อ map ไปยัง section คนที่ 1 / คนที่ 2 ได้แน่นอน
- ห้าม hardcode ว่า record แรกจาก query คือ guardian คนที่ 1 เพราะเปราะและผิดง่าย

### **B: Missing name split**
แบบร่างแยก `ชื่อ` และ `นามสกุล` ดังนั้น schema ต้องแยก field จริงทั้งฝั่ง student และ guardian

ข้อกำหนดที่ควรใช้:
- backend model ต้องมี first name / last name แยก
- request/response DTO ต้องส่งค่าแยก
- frontend form state ต้อง bind ค่าแยก ไม่ใช่ให้ component document มานั่ง parse เอง
- คำตอบที่ซื่อสัตย์คือ **split ชื่อไทยจาก full name อัตโนมัติไม่ reliable พอ** จึงไม่ควรใช้เป็นแนวทางหลัก

### **C: Missing Line ID semantics**
ได้ข้อสรุปแล้วว่า `Line ID` ในเอกสารหมายถึง `LineUserId` ที่ระบบใช้ผูกบัญชี LINE ในแอป

ข้อกำหนดที่ควรใช้:
- ต้องกำหนดชัดว่าใน model นี้เป็น system identifier ที่ยอมให้แสดงในเอกสารได้
- document DTO ควรใช้ชื่อ field ให้ชัด เช่น `lineUserId`
- registration/profile flow ต้องรู้ว่าจะ populate field นี้จากการ login binding ไม่ใช่กรอกมือมั่วในฟอร์ม

---

## 🔒 5. Security / PDPA Rules
- เอกสารนี้มีข้อมูลส่วนบุคคลหลายจุด ต้องใช้ authorization แบบเดียวกับข้อมูล profile
- ห้าม expose ข้อมูลเอกสารของคนอื่นผ่าน query string ที่เดาได้ง่าย
- รูปภาพในเอกสารยังต้องดึงผ่าน protected photo endpoints หรือ signed/authorized document payload ที่ปลอดภัย
- ถ้าจะรองรับ admin export ภายหลัง ต้องมี role/authorization แยก ไม่ใช่ reuse endpoint เดิมแบบหลวม ๆ

---

## 🛠️ 6. File-Level Implementation Checklist

### **A: Database / persistence checklist**
- [ ] ออกแบบการเปลี่ยน schema `Students` ให้รองรับชื่อ/นามสกุลแยกจริง
- [ ] ออกแบบการเปลี่ยน schema `Guardians` ให้รองรับผู้ปกครอง 2 คนต่อ 1 นักเรียน
- [ ] เพิ่ม column/field ที่จำเป็นสำหรับ guardian ordering หรือ guardian role
- [ ] วาง migration หรือ bootstrap path สำหรับข้อมูลเดิม
- [ ] กำหนด strategy สำหรับ backfill ข้อมูลเดิมที่เคยเก็บเป็น full name

### **B: Backend model / repository checklist**
- [ ] แก้ `CsrApi/Models/Student.cs`
  - เพิ่ม first name / last name แยก
  - รักษาหลักการเข้ารหัสข้อมูลส่วนบุคคลให้เหมือน field เดิม
- [ ] แก้ `CsrApi/Models/Guardian.cs`
  - เพิ่ม first name / last name แยก
  - เพิ่ม field สำหรับลำดับหรือชนิด guardian
  - คง `LineUserId` semantics ให้ชัด
- [ ] แก้ `CsrApi/Repositories/StudentRepository.cs`
  - ปรับ insert/update/select ให้รองรับ student name split และ guardian 2 คน
  - แยก helper mapping ไม่ให้ method ยาวเกินไป
- [ ] ถ้ามี seed/dev bootstrap ให้ปรับข้อมูลตัวอย่างให้ครบตาม model ใหม่

### **C: Request / response contract checklist**
- [ ] แก้ `CsrApi/Models/RegistrationRequest.cs`
  - student ใช้ `firstName` / `lastName`
  - guardian เปลี่ยนจาก object เดียวเป็น 2 entries ที่ชัดเจน
- [ ] แก้ `CsrApi/Models/ProfileResponse.cs`
  - สะท้อน student name split
  - สะท้อน guardian 2 คน
  - สะท้อน `lineUserId` ตาม semantics ใหม่
- [ ] เพิ่ม DTO ใหม่สำหรับ document เช่น `IntroductionDocumentResponse`
  - รวมข้อมูลที่หน้า print ต้องใช้เท่านั้น
  - ไม่ปน logic ภายในเกินจำเป็น

### **D: Service / endpoint checklist**
- [ ] แก้ `CsrApi/Services/RegistrationService.cs`
  - ปรับ upsert flow ให้รองรับ guardian 2 คน
  - ปรับ profile mapping ให้ส่งข้อมูลชื่อ/นามสกุลแยก
  - แยก function ย่อยสำหรับ map student, guardians, document response
- [ ] แก้ `CsrApi/Program.cs`
  - เพิ่มหรือปรับ endpoint สำหรับ document view
  - คง authorization rule เดิมแบบ `/api/me`
- [ ] ตรวจสอบ endpoint รูปภาพเดิมให้ยังใช้ร่วมกับ document preview ได้

### **E: Frontend form / state checklist**
- [ ] แก้ `csr-frontend/src/composables/useRegistrationForm.js`
  - student state เปลี่ยนเป็นชื่อ/นามสกุลแยก
  - guardian state เปลี่ยนเป็น guardian 2 คน
  - load/apply profile รองรับ contract ใหม่
- [ ] แก้ `csr-frontend/src/services/registrationApi.js`
  - ปรับ payload ให้ส่งชื่อ/นามสกุลแยก
  - ปรับ payload ให้ส่ง guardian 2 คน
  - เพิ่ม function fetch document payload
- [ ] แก้ `csr-frontend/src/views/Register.vue`
  - bind field ใหม่โดยไม่ยัด logic หนักไว้ใน view
  - กระจายเป็น section/component ถ้าหน้าเริ่มยาวเกินไป

### **F: Frontend document view checklist**
- [ ] เพิ่ม `csr-frontend/src/composables/useIntroductionDocument.js`
- [ ] เพิ่ม `csr-frontend/src/views/IntroductionDocumentView.vue`
- [ ] เพิ่ม component ย่อยสำหรับ photo row / student section / guardian section
- [ ] ทำ print stylesheet ให้รองรับ A4 และ `window.print()`
- [ ] ทดสอบกรณี guardian คนที่ 2 ไม่มีรูปหรือไม่มีข้อมูลบาง field

### **G: Verification checklist**
- [ ] ทดสอบ registration flow ใหม่กับข้อมูล guardian 2 คน
- [ ] ทดสอบ `/api/me` และ document endpoint กับข้อมูลจริง
- [ ] ทดสอบ print/export PDF จาก browser บน layout A4
- [ ] ทดสอบว่าการเปลี่ยน model ไม่ทำให้ secure photo access ถอยหลัง

---

## 🧪 7. Acceptance Criteria
- มีหน้า preview เอกสารที่จัดวางใกล้เคียง `design/306633.jpg`
- หน้า preview ใช้ข้อมูลจริงจาก API ไม่ใช่ mock data ฝังใน component
- registration schema และ payload รองรับ guardian 2 คนจริง
- student และ guardian ใช้ชื่อ/นามสกุลแยกจริงใน model และ DTO
- `Line ID` ที่แสดงในเอกสารถูก map จาก `LineUserId` ของระบบอย่างชัดเจน
- ผู้ใช้สามารถกดพิมพ์/บันทึกเป็น PDF ได้จาก browser
- layout ตอน print ไม่แตกและคุมขนาดบน A4 ได้สมเหตุสมผล
- หากข้อมูลบางช่องยังไม่มี ระบบต้องแสดงผลแบบ graceful โดยไม่ทำให้ layout พัง
- access control ของข้อมูลและรูปภาพยังไม่ถอยหลังด้าน security
- logic fetching, mapping, rendering และ export ไม่กระจุกเป็นไฟล์ก้อนเดียว

---

## 🧭 8. Recommended Delivery Order
1. ปรับ schema และ repository ให้รองรับ guardian 2 คน + name split
2. ปรับ registration/profile contract ให้สอดคล้องกับ model ใหม่
3. migrate หรือ backfill ข้อมูลเดิมเท่าที่ทำได้อย่างปลอดภัย
4. ทำ document endpoint สำหรับ view/export
5. ทำหน้า preview แบบ print-friendly
6. เพิ่มปุ่ม print/export PDF
7. ทดสอบกับข้อมูลจริงและกรณี field ไม่ครบ

---

## 🚫 9. Out of Scope for the First Pass
- batch export หลายคนพร้อมกัน
- backend-generated PDF ที่ pixel-perfect
- electronic signature
- drag-and-drop document designer
- admin backoffice สำหรับค้นหาและ export เอกสารทั้งห้อง
