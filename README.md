# 🏫 Classroom Student Registry (CSR) Project Blueprint
**Target:** ระบบจัดการทะเบียนนักเรียนและเครือข่ายผู้ปกครอง (ม.1/2 สวนกุหลาบนนท์)
**Stack:** .NET 10 Web API + SQLite (SQLCipher) + Vue.js 3 (LIFF)
**Compliance:** PDPA Standard & Data Masking Logic

---

## 🏗️ 1. Project Overview
ระบบเว็บแอปพลิเคชันภายใน LINE (LIFF) เพื่ออำนวยความสะดวกในการจัดเก็บและเข้าถึงข้อมูลนักเรียน โดยเน้นความปลอดภัยของข้อมูลส่วนบุคคล (PDPA) ตามโครงสร้างข้อมูลจากไฟล์ Excel ปีการศึกษา 2561

---

## 🗃️ 2. Database Schema (SQLite)

### **Table: `Students`**
| Column | Type | Constraints | Logic / Security |
| :--- | :--- | :--- | :--- |
| `Id` | GUID | PRIMARY KEY | - |
| `StudentId` | TEXT | UNIQUE | รหัสประจำตัว (เช่น 30558) |
| `OldRoom` | TEXT | - | ห้องเดิมตอน ม.1 |
| `OldNo` | INTEGER | - | เลขที่เดิม |
| `NewRoom` | TEXT | - | ห้องใหม่ (ม.1/2) |
| `NewNo` | INTEGER | - | เลขที่ใหม่ |
| `EncName` | TEXT | NOT NULL | ชื่อ-นามสกุลจริง (**AES-256**) |
| `Nickname` | TEXT | - | ชื่อเล่น |
| `BloodType` | TEXT | - | กรุ๊ปเลือด (**Encrypted**) |
| `Status` | TEXT | - | สถานะการเรียน (ปกติ/ร/มผ) |

### **Table: `Guardians`**
| Column | Type | Constraints | Logic / Security |
| :--- | :--- | :--- | :--- |
| `Id` | GUID | PRIMARY KEY | - |
| `StudentId` | GUID | FK | เชื่อมกับ Students |
| `Relation` | TEXT | - | บิดา / มารดา / ผู้ปกครอง |
| `EncName` | TEXT | - | ชื่อ-นามสกุล (**AES-256**) |
| `EncPhone` | TEXT | - | เบอร์โทรศัพท์ (**AES-256**) |
| `LineUserId` | TEXT | UNIQUE | LINE ID จาก LIFF สำหรับ Login |

### **Table: `Committees`**
| Column | Type | Constraints | Logic / Security |
| :--- | :--- | :--- | :--- |
| `Position` | TEXT | - | ประธาน / ครู / หัวหน้าห้อง |
| `EncName` | TEXT | - | ชื่อ (**AES-256**) |
| `EncPhone` | TEXT | - | เบอร์ติดต่อ (**AES-256**) |

---

## 🔒 3. Security & PDPA Implementation

### **Data Access Rules**
1. **Encryption at Rest:** ข้อมูลที่ระบุว่าเป็น `Enc` จะถูกเข้ารหัสด้วย AES-256 ก่อนบันทึกลง SQLite
2. **Masking at UI:** 
   - ชื่อจริงจะแสดงเป็น: `นาย รัชพล ภู*******`
   - เบอร์โทรจะแสดงเป็น: `081-XXX-5678`
3. **Identity Verification:** ผู้ปกครองต้องกรอก `StudentId` + `OldRoom` + `OldNo` เพื่อทำการ Binding LINE ID ในครั้งแรก

---

## 📱 4. Frontend Architecture (Vue.js 3 + Tailwind)

### **Key Screens**
- **Auth:** `Login.vue` (LIFF Integration) & `Consent.vue` (PDPA Agreement)
- **Home:** `Dashboard.vue` (สรุปสถานะนักเรียนรายบุคคล)
- **Directory:** `ClassList.vue` (รายชื่อเพื่อนในห้องแบบ Masked Name)
- **Contacts:** `CommitteeList.vue` (เบอร์ติดต่อครูและกรรมการ พร้อมปุ่ม Click-to-Call)

### **Frontend Logic (Masking Utilities)**
```javascript
// Example: src/utils/masking.js
export const maskPhone = (phone) => phone?.replace(/(\d{3})\d{4}(\d{3})/, '$1-XXX-$2');
export const maskName = (name) => {
  const [first, ...rest] = name.split(' ');
  return `${first} ${rest.join(' ').substring(0, 1)}*******`;
};