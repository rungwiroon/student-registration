# 🏫 Project Blueprint: Classroom Management System (LIFF + SQLite)
**Context:** ระบบจัดการทะเบียนนักเรียน ม.2/11 (ข้อมูลอ้างอิงปีการศึกษา 2561)
**Compliance:** PDPA Focused with Multi-layer Encryption

---

## 🗃️ 1. Database Schema (SQLite)
โครงสร้างตารางที่ออกแบบมาเพื่อรองรับข้อมูลที่ซับซ้อนจาก Excel โดยตรง

### Table: `Students` (ข้อมูลหลักนักเรียน)
| Column | Type | Constraints | Source Field / Logic |
| :--- | :--- | :--- | :--- |
| `Id` | GUID | PRIMARY KEY | System Generated |
| `StudentId` | TEXT | UNIQUE | รหัสประจำตัว (เช่น 30558) |
| `OldRoom` | TEXT | - | ห้องเดิม (จากไฟล์ "ห้องเรียนใหม่") |
| `OldNo` | INTEGER | - | เลขที่เดิม |
| `NewRoom` | TEXT | - | ห้องใหม่ (เช่น ม.2/11) |
| `NewNo` | INTEGER | - | เลขที่ใหม่ (จากไฟล์ "ม.2-11") |
| `EncryptedName` | TEXT | NOT NULL | ชื่อ-นามสกุลจริง (**AES-256**) |
| `Nickname` | TEXT | - | ชื่อเล่น |
| `BloodType` | TEXT | - | กรุ๊ปเลือด (Sensitive) |
| `DOB` | TEXT | - | วันเดือนปีเกิด (เช่น 26/02/2548) |
| `EncryptedPhone` | TEXT | - | เบอร์โทรศัพท์นักเรียน (**Encrypted**) |
| `Status` | TEXT | - | สถานะ (ปกติ/ลาออก/ร/มผ) |

### Table: `Guardians` (ข้อมูลผู้ปกครอง)
*รองรับบิดาและมารดาแยกกันตามไฟล์ต้นฉบับ*
| Column | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Id` | GUID | PRIMARY KEY | - |
| `StudentId` | GUID | FOREIGN KEY | เชื่อมกับ Students |
| `RelationType` | TEXT | - | 'Father', 'Mother', 'Other' |
| `EncryptedName` | TEXT | - | ชื่อผู้ปกครอง (**Encrypted**) |
| `EncryptedPhone` | TEXT | - | เบอร์โทรศัพท์ (**Encrypted**) |
| `Occupation` | TEXT | - | อาชีพ (เช่น รับราชการ, ธุรกิจส่วนตัว) |
| `Email` | TEXT | - | อีเมล |
| `LineUserId` | TEXT | UNIQUE | ใช้ทำ Authentication (LIFF) |

### Table: `Committees` (ตารางกรรมการเครือข่าย)
| Column | Type | Constraints | Source Field |
| :--- | :--- | :--- | :--- |
| `Position` | TEXT | - | ประธาน, เลขานุการ, หัวหน้าห้อง, ครูที่ปรึกษา |
| `EncryptedName` | TEXT | - | ชื่อกรรมการ (**Encrypted**) |
| `EncryptedPhone` | TEXT | - | เบอร์ติดต่อ (**Encrypted**) |
| `LineIdDisplay` | TEXT | - | LINE ID ที่อนุญาตให้สมาชิกเห็น |

---

## 🔒 2. Data Protection Strategy (PDPA)

### **Multi-Layer Security**
1. **At-Rest:** ใช้ **SQLCipher** เข้ารหัสไฟล์ `.db` และใช้ **AES-256** เข้ารหัสเฉพาะฟิลด์ (Column-level)
2. **In-Transit:** ข้อมูลออกจาก API จะต้องผ่าน `MaskingService` เสมอ
   - *ชื่อ:* เด็กชายปภังกร พ********
   - *เบอร์โทร:* 081-XXX-2921
3. **Identity-Verification:** เมื่อผู้ปกครองเข้าผ่าน LINE ครั้งแรก ต้องระบุ "เลขประจำตัวนักเรียน" + "วันเกิด" เพื่อทำการ Binding กับ `LineUserId`

---

## 📱 3. Feature Highlights (LINE Integration)
- **Class Directory:** รายชื่อเพื่อนในห้องพร้อมเบอร์ติดต่อกรรมการ (แสดงผลแบบ Mobile-friendly ใน LINE Browser)
- **Call-to-Action:** ปุ่มกดโทรออกหาครูที่ปรึกษาหรือกรรมการห้องได้ทันทีจากแอป
- **Status Tracking:** ตรวจสอบสถานะติด "0, ร, มผ, มส" รายบุคคล (ข้อมูลจาก Sheet 0)
- **Accounting:** หน้าแสดงเลขบัญชีห้องและประวัติการโอนเงินค่ากิจกรรม (เพื่อความโปร่งใส)

---

## 🛠️ 4. Development Steps for Antigravity

1. **Step 1: Setup Persistence Layer**
   - ใช้ `Microsoft.Data.Sqlite` และ `LanguageExt` เพื่อทำ Functional Data Access
   - สร้าง `EncryptionService` สำหรับจัดการ AES-256 Key

2. **Step 2: Excel Parser Service**
   - เขียน Logic การ Parse ข้อมูลจาก CSV/Excel ที่มีหัวตารางแบบ "Confidential"
   - จัดการข้อมูลที่อยู่ใน Cell เดียวกัน (เช่น ชื่อผู้ปกครอง 2 คนในหนึ่งช่อง) ให้แยกเข้าตาราง `Guardians`

3. **Step 3: LIFF Auth Middleware**
   - ตรวจสอบ `Line-Access-Token` และดึง `userId` มาเทียบกับตาราง `Guardians` หรือ `Committees`

4. **Step 4: PDPA UI**
   - ทำหน้าจอขอความยินยอม (Consent) และระบบ Masking ข้อมูลบน Vue.js/Tailwind

---

## 💡 Prompt สำหรับ Antigravity (Copy-Ready)
> "Generate a .NET 10 project with SQLite. 
> Define entities for Student, Guardian, and Committee based on the provided markdown schema. 
> Use AES-256 encryption for all 'Encrypted' fields. 
> Implement a Result Pattern using LanguageExt for the API. 
> Focus on a service that can import the CSV data where student and parent info are mixed in strings."