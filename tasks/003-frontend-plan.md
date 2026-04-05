# 🏫 Master Blueprint: Classroom Student Registry (CSR)
**Context:** ระบบจัดการทะเบียนนักเรียนและเครือข่ายผู้ปกครอง (อ้างอิงข้อมูล ม.1/2 ปีการศึกษา 2561)
**Tech Stack:** .NET 10 Web API + SQLite + Vue.js 3 + Tailwind CSS + LINE LIFF
**Focus:** Mobile-First UI & Strict PDPA Compliance (Data Masking & Encryption)

---

## 🏗️ 1. System Architecture & Tech Stack
*   **Frontend:** Vue.js 3 (Composition API) + Tailwind CSS (Mobile-First 480px max-width)
*   **Authentication:** LINE Login ผ่าน LIFF SDK (ใช้ `userId` ผูกกับข้อมูลนักเรียน)
*   **Backend:** .NET 10 Minimal APIs
*   **Logic Pattern:** Railway-Oriented Programming (ใช้ `LanguageExt` สำหรับ Result pattern)
*   **Database:** SQLite (เข้ารหัสไฟล์ระดับดิสก์ด้วย SQLCipher)

---

## 🗃️ 2. Database Schema (SQLite)

### **Table: `Students`** (ข้อมูลนักเรียนและการย้ายห้อง)
| Column | Type | Constraints | Description / Security |
| :--- | :--- | :--- | :--- |
| `Id` | GUID | PRIMARY KEY | System Generated |
| `StudentId` | TEXT | UNIQUE | รหัสประจำตัว (เช่น 30558) |
| `OldRoom` | TEXT | - | ม.1 ไม่มีห้องเดิม |
| `OldNo` | INTEGER | - | ม.1 ไม่มีเลขที่เดิม |
| `NewRoom` | TEXT | - | ห้องใหม่ (ม.1/2) |
| `NewNo` | INTEGER | - | เลขที่ใหม่ |
| `EncName` | TEXT | NOT NULL | ชื่อ-นามสกุลจริง (**AES-256 Encrypted**) |
| `Nickname` | TEXT | - | ชื่อเล่น |
| `BloodType` | TEXT | - | กรุ๊ปเลือด (**Encrypted**) |
| `DOB` | TEXT | - | วันเดือนปีเกิด |
| `EncPhone` | TEXT | - | เบอร์โทรศัพท์นักเรียน (**AES-256 Encrypted**) |
| `Status` | TEXT | - | สถานะปัจจุบัน (ปกติ/ลาออก) รวมถึงประวัติ ร/มผ |

### **Table: `Guardians`** (ข้อมูลผู้ปกครอง)
| Column | Type | Constraints | Description / Security |
| :--- | :--- | :--- | :--- |
| `Id` | GUID | PRIMARY KEY | - |
| `StudentId` | GUID | FOREIGN KEY | เชื่อมกับตาราง Students |
| `Relation` | TEXT | - | บิดา / มารดา / ผู้ปกครองอื่นๆ |
| `EncName` | TEXT | NOT NULL | ชื่อ-นามสกุล (**AES-256 Encrypted**) |
| `EncPhone` | TEXT | NOT NULL | เบอร์โทรศัพท์ (**AES-256 Encrypted**) |
| `Occupation` | TEXT | - | อาชีพ (จากไฟล์ Excel) |
| `LineUserId` | TEXT | UNIQUE | LINE ID (ได้จาก LIFF SDK) สำหรับ Auto-login |

### **Table: `Committees`** (คณะกรรมการเครือข่าย)
| Column | Type | Constraints | Description / Security |
| :--- | :--- | :--- | :--- |
| `Id` | INTEGER | PRIMARY KEY | AUTOINCREMENT |
| `Position` | TEXT | - | ตำแหน่ง (ประธาน, เลขานุการ, หัวหน้าห้อง, ครูที่ปรึกษา) |
| `EncName` | TEXT | NOT NULL | ชื่อ-นามสกุล (**AES-256 Encrypted**) |
| `EncPhone` | TEXT | NOT NULL | เบอร์ติดต่อ (**AES-256 Encrypted**) |

---

## 🔒 3. PDPA & Security Logic (Data Masking)

### **A. Encryption at Rest (Backend)**
*   ไฟล์ `database.db` ถูกเข้ารหัสทั้งก้อนด้วย **SQLCipher**
*   ฟิลด์ที่ขึ้นต้นด้วย `Enc...` จะถูกเข้ารหัสด้วย **AES-256** ก่อน Insert ลง Database เสมอ

### **B. Data Masking (Frontend Utilities)**
ข้อมูลที่ส่งผ่าน API จะถูก Mask ก่อนแสดงผลเสมอ (เว้นแต่เป็นข้อมูลของลูกตัวเอง):
```javascript
// src/utils/masking.js
export const maskPhone = (phone) => {
  if (!phone || phone.length < 10) return phone;
  return phone.replace(/(\d{3})\d{4}(\d{3})/, '$1-XXX-$2'); // 081-XXX-2921
};

export const maskFullName = (name) => {
  if (!name) return '';
  const parts = name.split(' ');
  if (parts.length < 2) return name.substring(0, 3) + '****';
  return `${parts[0]} ${parts[1].substring(0, 1)}*******`; // เด็กชายปภังกร พ********
};

📱 4. Frontend Architecture (Vue 3)
Folder Structure

src/
├── components/
│   ├── Layout/BottomNav.vue
│   └── Shared/MaskedText.vue
├── composables/
│   └── useLiff.js      # จัดการ LIFF Init และ Auth
├── views/
│   ├── Auth/Login.vue
│   ├── Auth/Register.vue  # ฟอร์มลงทะเบียนนักเรียนและผู้ปกครอง
│   ├── App/Dashboard.vue  # ข้อมูลส่วนตัวและสถานะ ร/มผ
│   ├── App/ClassList.vue  # สมุดรายชื่อเพื่อน (Masked)
│   └── App/Contacts.vue   # เบอร์โทรครู/กรรมการ (Click-to-call)
└── utils/masking.js

🤖 5. Prompts for Antigravity AI (Copy & Paste)
Step 1: Backend & Database (C# .NET 10)
"Create a .NET 10 Web API project using SQLite and SQLCipher. Define Entity Models for 'Students', 'Guardians', and 'Committees' based strictly on the provided markdown schema. Implement AES-256 encryption logic for all fields starting with 'Enc'. Use LanguageExt's 'Either' monad for all service and repository responses to ensure functional error handling. Include an endpoint that accepts a JSON payload to seed the initial data from an Excel export."

Step 2: Frontend Layout & Auth (Vue 3)
"Scaffold a Vue 3 Composition API project named 'csr-frontend' with Tailwind CSS. Create a mobile-first layout (max-width: 480px, centered) with a sticky Header and a fixed Bottom Navigation bar. Create a Vue composable named useLiff.js using @line/liff to handle LINE login and retrieve the user's profile. Build a 'Register' form view where parents input Student and Guardian data, and submit to POST /api/register."

Step 3: Frontend Views & Masking
"Implement a masking.js utility with functions to mask Thai phone numbers (e.g., 081-XXX-2921) and Thai full names (keeping only the first name and the first letter of the last name). Create a ClassList.vue component that lists students, applying these masking functions. Create a Contacts.vue component for the committee members with a clickable 'href=tel:' button."