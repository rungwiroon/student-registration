# 🔐 Phase 9: Backoffice Role Access Policy & Wireframe
**Status:** Planned
**Depends on:** `tasks/010-admin-page-plan.md` (ถือว่า first slice ของ backoffice เสร็จแล้ว)
**Goal:** ล็อกนโยบายสิทธิ์เข้าถึงของฝั่ง backoffice ให้ชัดสำหรับ `Teacher` และ `ParentNetworkStaff` ทั้งในระดับ API, ข้อมูลที่มองเห็นได้, การเข้าถึงรูป/เอกสาร, การแก้ `ReviewStatus` และออกแบบ wireframe สำหรับหน้าที่ต้องสะท้อนความต่างของสิทธิ์เหล่านี้

---

## 🎯 1. Problem Statement
หลังจากมี backoffice first slice แล้ว งานถัดไปที่ต้องล็อกให้ชัดคือ **ใครเห็นอะไรได้บ้าง และแก้อะไรได้บ้าง**

ตอนนี้ requirement ที่ตกลงกันแล้วมีแกนหลักดังนี้:
- `Teacher`
  - ดูข้อมูลเต็มได้
  - ดูรูป/เอกสารได้
  - แก้ `ReviewStatus` ได้
- `ParentNetworkStaff`
  - เริ่มจาก read-only ก่อน
  - ถ้ายังไม่ชัดเรื่อง PDPA ให้พิจารณา limited view หรือ masked บาง field
  - ค่อยปลดสิทธิ์เพิ่มทีหลัง

ถ้าไม่แตกงานนี้ออกมาเป็น task แยก จะเกิดปัญหาตรง ๆ:
- backend อาจคืนข้อมูลเกินสิทธิ์เพราะใช้ DTO เดียวกันทุก role
- frontend อาจแค่ซ่อนปุ่ม แต่ยังดึงข้อมูลเต็มมาแล้ว ซึ่งไม่ปลอดภัย
- การดูรูป/เอกสารอาจเปิดกว้างเกินจำเป็น
- policy จะกระจายอยู่หลายไฟล์และ maintain ยาก

สรุปสั้น ๆ:
- งานนี้คือ **policy task + contract task + UI task**
- ต้องแก้ที่ data access จริง ไม่ใช่แก้เฉพาะหน้าจอ

---

## 🧩 2. Target Role Model

### **A: Teacher**
สิทธิ์ขั้นต่ำที่ต้องมี:
- ดูข้อมูลนักเรียนเต็ม
- ดูข้อมูลผู้ปกครองเต็ม
- ดูรูปนักเรียนและรูปผู้ปกครองได้
- ดู introduction document ได้
- แก้ `ReviewStatus` ได้
- เพิ่ม internal note ได้ ถ้าระบบ note พร้อมใช้งานแล้ว

### **B: ParentNetworkStaff**
สิทธิ์เริ่มต้นที่แนะนำ:
- ดูรายการนักเรียนได้
- เปิดดูรายละเอียดรายคนได้แบบ **read-only**
- ไม่มีสิทธิ์แก้ `ReviewStatus`
- ไม่มีสิทธิ์แก้ note
- การดูรูป/เอกสารให้เริ่มแบบ conservative ก่อน จนกว่าจะเคลียร์ PDPA

ข้อแนะนำแบบตรง ๆ:
- อย่าให้ `ParentNetworkStaff` เห็นข้อมูลเต็มทุก field ตั้งแต่แรก ถ้ายังไม่มีข้อสรุป PDPA
- เริ่มจาก limited view ก่อน แล้วค่อยขยายทีหลัง จะปลอดภัยกว่าและย้อนกลับง่ายกว่า

### **C: Future expansion (not in this task)**
เก็บไว้เผื่ออนาคต:
- `SystemAdmin`
- `ReadonlyAuditor`
- `HomeroomTeacherOnly` หรือ role เฉพาะห้อง

---

## 🔒 3. Proposed Access Policy Matrix

| Capability | Teacher | ParentNetworkStaff |
| :--- | :--- | :--- |
| View student list | Yes | Yes |
| View student full name | Yes | Yes |
| View full guardian profile | Yes | Limited / Masked by default |
| View student phone | Yes | Limited / Masked by default |
| View guardian phone | Yes | Masked by default |
| View student photo | Yes | Pending PDPA decision |
| View guardian photo | Yes | Pending PDPA decision |
| View introduction document | Yes | Pending PDPA decision |
| Update `ReviewStatus` | Yes | No |
| Add/edit internal note | Yes | No |
| Access status history / audit | Optional | No |

### **Default rule if requirement is unclear**
- ให้ `Teacher` ได้ full access ตามหน้าที่
- ให้ `ParentNetworkStaff` ใช้ **read-only + limited view** เป็น default
- ถ้ายังเถียงกันเรื่อง PDPA อยู่ ให้ mask field ที่ sensitive ไว้ก่อน

---

## 🛡️ 4. PDPA-Oriented Data Classification
เพื่อให้ implement ได้ไม่มั่ว ควรแบ่ง field ตามระดับความอ่อนไหว

### **A: Low sensitivity**
ตัวอย่าง:
- ชื่อ-นามสกุลนักเรียน
- ห้อง
- เลขที่
- สถานะการ review

### **B: Medium sensitivity**
ตัวอย่าง:
- ชื่อผู้ปกครอง
- ความสัมพันธ์
- อาชีพ
- การมี/ไม่มีรูป

### **C: High sensitivity**
ตัวอย่าง:
- เบอร์โทรนักเรียน
- เบอร์โทรผู้ปกครอง
- รูปนักเรียน
- รูปผู้ปกครอง
- document ที่รวมข้อมูลหลายจุดในหน้าเดียว
- `LineUserId`

### **Policy recommendation**
- `Teacher` เห็น Low/Medium/High ได้
- `ParentNetworkStaff` เห็น Low ได้แน่
- Medium/High ให้เริ่มจาก masked หรือ deny-by-default จนกว่าจะมี business sign-off

---

## ⚙️ 5. Backend Design Changes Required

### **A: Role-aware authorization service**
ต้องมี service กลางที่ตอบคำถามได้แบบชัดเจน เช่น:
- user นี้เป็น `Teacher` หรือ `ParentNetworkStaff`
- role นี้เรียก endpoint นี้ได้ไหม
- role นี้ดู field ระดับนี้ได้ไหม

### **B: Role-aware DTO strategy**
ห้ามใช้ detail DTO ชุดเดียวแล้วค่อยซ่อนปุ่มฝั่ง frontend

แนวทางที่ควรใช้:
- option 1: แยก DTO ตาม role
  - `TeacherStudentDetailResponse`
  - `ParentNetworkStudentDetailResponse`
- option 2: ใช้ DTO เดียว แต่ backend เป็นคน mask field ก่อนส่ง

ข้อแนะนำ:
- ถ้าความต่างของข้อมูลไม่เยอะ ใช้ DTO เดียว + backend masking ได้
- ถ้าความต่างเริ่มเยอะ อย่าฝืน ควรแยก DTO เพื่อความชัดเจน

### **C: Endpoint-level policy**
ตัวอย่าง policy:
- `GET /api/backoffice/students`
  - `Teacher`: full list item
  - `ParentNetworkStaff`: list item แบบ limited ถ้าจำเป็น
- `GET /api/backoffice/students/{id}`
  - `Teacher`: full detail
  - `ParentNetworkStaff`: masked detail
- `PATCH /api/backoffice/students/{id}/review-status`
  - `Teacher`: allow
  - `ParentNetworkStaff`: deny (`403`)
- `GET /api/backoffice/students/{id}/student-photo`
  - `Teacher`: allow
  - `ParentNetworkStaff`: pending decision, default deny
- `GET /api/backoffice/students/{id}/document`
  - `Teacher`: allow
  - `ParentNetworkStaff`: pending decision, default deny

### **D: Error contract**
- `401` ไม่มี token หรือ token ไม่ valid
- `403` login แล้วแต่ role ไม่มีสิทธิ์
- `404` ไม่พบ resource
- `200` พร้อม masked payload เมื่อ role เข้าถึงได้บางส่วน

---

## 🖥️ 6. UI Surfaces That Must Reflect Role Differences

### **A: Backoffice dashboard**
ต้องแสดง role ปัจจุบันให้ชัด เช่น:
- `Teacher`
- `Parent Network Staff (Read-only)`

### **B: Student list**
สิ่งที่ UI ต้องสะท้อน:
- role badge
- บาง column ถูกซ่อนหรือ masked ตาม role
- ไม่มี action button แก้สถานะสำหรับ `ParentNetworkStaff`

### **C: Student detail**
สิ่งที่ UI ต้องสะท้อน:
- field ที่อ่านได้เต็มสำหรับ `Teacher`
- field ที่ถูก mask หรือแทนด้วยข้อความ เช่น `ไม่มีสิทธิ์ดูข้อมูลนี้` สำหรับ `ParentNetworkStaff`
- ส่วน `ReviewStatus` editable เฉพาะ `Teacher`
- ปุ่ม document/photo ต้อง disabled หรือไม่ render สำหรับ role ที่ไม่มีสิทธิ์

### **D: Unauthorized / limited access state**
ต้องมี UI ที่แยกชัดระหว่าง:
- ไม่มีสิทธิ์เข้าหน้านี้เลย
- เข้าหน้านี้ได้ แต่บางข้อมูลถูกจำกัด

---

## 🧱 7. Wireframes

### **A: Backoffice header / identity bar**
```text
+--------------------------------------------------------------------------------+
| CSR Backoffice                                              [Teacher ▼]       |
| Role: Teacher | Access: Full Student Review                                   |
+--------------------------------------------------------------------------------+
```

สำหรับ `ParentNetworkStaff`
```text
+--------------------------------------------------------------------------------+
| CSR Backoffice                            [Parent Network Staff ▼]            |
| Role: Read-only | Some personal fields are masked for privacy                 |
+--------------------------------------------------------------------------------+
```

### **B: Student list - Teacher view**
```text
+--------------------------------------------------------------------------------+
| Search [_____________] Room [v] Review Status [v]                             |
+--------------------------------------------------------------------------------+
| No | Student Name     | Room | Review Status | Guardian Phone | Actions       |
|----|------------------|------|---------------|----------------|---------------|
| 01 | ด.ช. สมชาย ใจดี   | 1/2  | Pending       | 08x-xxx-1234   | [Open] [Edit] |
| 02 | ด.ญ. สมหญิง ดีใจ  | 1/2  | Verified      | 09x-xxx-5678   | [Open] [Edit] |
+--------------------------------------------------------------------------------+
```

### **C: Student list - ParentNetworkStaff view**
```text
+--------------------------------------------------------------------------------+
| Search [_____________] Room [v]                                                |
+--------------------------------------------------------------------------------+
| No | Student Name     | Room | Review Status | Guardian Phone | Actions       |
|----|------------------|------|---------------|----------------|---------------|
| 01 | ด.ช. สมชาย ใจดี   | 1/2  | Pending       | 08x-xxx-****   | [Open]        |
| 02 | ด.ญ. สมหญิง ดีใจ  | 1/2  | Verified      | 09x-xxx-****   | [Open]        |
+--------------------------------------------------------------------------------+
| Note: Some information is masked for privacy.                                  |
+--------------------------------------------------------------------------------+
```

### **D: Student detail - Teacher view**
```text
+--------------------------------------------------------------------------------+
| Student Profile                                              [Print Document] |
+--------------------------------------------------------------------------------+
| Student Info                                                                      
| Name: สมชาย ใจดี                                                                  
| Phone: 081-234-5678                                                               
| Room: 1/2   No: 12                                                                
| Photo: [View]                                                                     
|                                                                                   
| Guardian 1                                                                        
| Name: นายใจดี ใจมั่น                                                              
| Phone: 089-111-2222                                                               
| Photo: [View]                                                                     
|                                                                                   
| Review Status: [ Pending v ]   [Save]                                            
| Internal Note: [______________________________________________] [Save Note]      |
+--------------------------------------------------------------------------------+
```

### **E: Student detail - ParentNetworkStaff view**
```text
+--------------------------------------------------------------------------------+
| Student Profile                                                                   |
+--------------------------------------------------------------------------------+
| Student Info                                                                      
| Name: สมชาย ใจดี                                                                  
| Phone: 081-xxx-xxxx                                                               
| Room: 1/2   No: 12                                                                
| Photo: ไม่มีสิทธิ์ดู                                                              
|                                                                                   
| Guardian 1                                                                        
| Name: นายใจดี ใจมั่น                                                              
| Phone: 089-xxx-xxxx                                                               
| Photo: ไม่มีสิทธิ์ดู                                                              
|                                                                                   
| Review Status: Pending                                                            
| Internal Note: ไม่มีสิทธิ์แก้ไข                                                   |
+--------------------------------------------------------------------------------+
```

### **F: Limited-access notice component**
```text
+------------------------------------------------------------------+
| ข้อมูลบางส่วนถูกปิดบังตามสิทธิ์การเข้าถึงและข้อกำหนด PDPA       |
+------------------------------------------------------------------+
```

---

## 🧪 8. Acceptance Criteria
- มี role policy ชัดเจนสำหรับ `Teacher` และ `ParentNetworkStaff`
- `Teacher` ดูข้อมูลเต็ม, ดูรูป/เอกสาร, และแก้ `ReviewStatus` ได้จริง
- `ParentNetworkStaff` เป็น read-only ใน first pass
- ถ้ายังไม่มี PDPA sign-off สำหรับข้อมูลอ่อนไหว ระบบต้องใช้ masked/limited view เป็นค่าเริ่มต้น
- backend ไม่ส่งข้อมูลเกินสิทธิ์โดยหวังให้ frontend ซ่อนเอง
- UI แสดงความต่างของสิทธิ์อย่างชัดเจน
- unauthorized action คืน `403` อย่างถูกต้อง
- มี wireframe เพียงพอให้เริ่มออกแบบ/implement UI ต่อได้

---

## 🛠️ 9. File-Level Implementation Checklist

### **A: Backend policy / authorization**
- [x] เพิ่ม policy helper สำหรับเช็ก capability เช่น view full data / view photo / update review status
- [x] แยกสิทธิ์ `Teacher` กับ `ParentNetworkStaff` ใน service layer
- [x] ล็อก default deny สำหรับ capability ที่ยังไม่เคลียร์ PDPA

### **B: Backend response shaping**
- [x] เลือก strategy ว่าจะใช้ role-specific DTO หรือ backend masking — ใช้ DTO เดียว + backend masking
- [x] ปรับ student detail response ให้รองรับ masked field
- [x] ปรับ list item response ให้รองรับ limited view
- [x] แยก endpoint `review-status` ให้ชัดถ้าจำเป็น — ใช้ policy check ใน PUT endpoint แทนการแยก route

### **C: Backend protected resource access**
- [x] ล็อกสิทธิ์การดูรูปนักเรียน
- [x] ล็อกสิทธิ์การดูรูปผู้ปกครอง
- [x] ล็อกสิทธิ์การดู document
- [ ] ทดสอบ default deny สำหรับ `ParentNetworkStaff` ก่อน

### **D: Frontend rendering**
- [x] แสดง role badge และ access level ใน backoffice layout
- [x] ปรับ student list ให้ render column/action ตาม role
- [x] ปรับ student detail ให้รองรับ editable vs read-only state
- [x] เพิ่ม limited-access notice component
- [x] ซ่อนหรือ disable ปุ่ม document/photo/review-status ตาม capability

### **E: Verification**
- [ ] ทดสอบ `Teacher` เปิด list/detail/photo/document/update status ได้จริง
- [ ] ทดสอบ `ParentNetworkStaff` เปิด list/detail ได้แบบ read-only
- [ ] ทดสอบ field masked ตรงตาม policy
- [ ] ทดสอบว่า frontend ไม่มีข้อมูลลึกค้างอยู่ใน payload ของ role ที่ไม่ควรเห็น
- [ ] ทดสอบ `403` สำหรับ action ที่ role ไม่มีสิทธิ์

---

## 🧭 10. Recommended Delivery Order
1. ล็อก capability matrix ให้ชัดใน backend
2. ตัดสินใจ DTO strategy: แยก DTO หรือ mask ใน backend
3. ล็อกสิทธิ์ photo/document สำหรับ `ParentNetworkStaff` เป็น default deny ก่อน
4. ปรับ list/detail payload ตาม role
5. ปรับ UI ให้สะท้อน read-only / limited-access state
6. ค่อยพิจารณาขยายสิทธิ์ `ParentNetworkStaff` ถ้ามี PDPA sign-off เพิ่ม

---

## 💥 11. Honest Risks / Caveats
- ถ้าคุณปล่อยให้ frontend เป็นคนซ่อนข้อมูลเอง งานนี้จะไม่ปลอดภัย
- ถ้า `ParentNetworkStaff` ได้ payload เต็มแล้วแค่ disabled ปุ่ม นั่นคือหลอกตัวเอง
- ถ้ายังไม่มีข้อสรุป PDPA เรื่องรูปและ document อย่าเปิดให้ role นี้ก่อน
- ถ้า `ReviewStatus` ยังใช้ field เดียวกับความหมายอื่นอยู่ ควรแยก semantics ให้ชัด
- ถ้า wireframe ไม่สะท้อน role differences ตั้งแต่ตอนนี้ UI implementation จะย้อนกลับมาเละทีหลัง

---

## ✅ 12. Suggested First Implementation Slice
- `Teacher` ใช้ full detail payload
- `ParentNetworkStaff` ใช้ read-only masked detail payload
- `PATCH /review-status` อนุญาตเฉพาะ `Teacher`
- photo/document endpoint default deny สำหรับ `ParentNetworkStaff`
- student list/detail UI แสดง role badge และ limited-access notice

slice นี้เล็กพอจะ implement ได้จริง และปลอดภัยกว่าการเปิดสิทธิ์กว้างแล้วค่อยหดกลับทีหลัง
