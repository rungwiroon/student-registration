# 🏫 Classroom Student Registry (CSR) Project Blueprint
**Target:** ระบบจัดการทะเบียนนักเรียนและเครือข่ายผู้ปกครอง (ม.1/2 สวนกุหลาบนนท์)
**Stack:** .NET 10 Web API + SQLite (SQLCipher) + Vue.js 3 (LIFF)
**Compliance:** PDPA Standard & Data Masking Logic

---

## 🚀 Docker Workflows

ระบบนี้แยก workflow เป็น `production-like` และ `development` อย่างชัดเจน

### **Production-like compose**
```bash
docker compose up --build
```

- Frontend: `http://localhost:5173`
- Backend API: `http://localhost:8080`

### **Development compose with hot reload**
```bash
docker compose -f docker-compose.dev.yml up --build
```

- Frontend ใช้ Vite dev server พร้อม hot reload
- Backend ใช้ `dotnet watch`
- dev stack เปิด mock auth และ seed data อัตโนมัติ
- dev stack ใช้ฐานข้อมูลแยกที่ `/app/data/csrapi-dev.db`

### **Environment setup**
- คัดลอก `.env.example` เป็น `.env` เมื่อต้องการตั้งค่าจริง
- ค่าที่สำคัญ:
  - `ENCRYPTION_KEY`
  - `LINE_LIFF_CHANNEL_ID`
  - `LINE_USE_MOCK_AUTH`
  - `LINE_MOCK_USER_ID`
  - `VITE_LIFF_ID`
  - `VITE_USE_MOCK_LIFF`
- สำหรับ local Docker ถ้ายังไม่ได้ต่อ LIFF จริง ให้เปิด mock ทั้งสองฝั่งให้ตรงกัน:
  - `LINE_USE_MOCK_AUTH=true`
  - `VITE_USE_MOCK_LIFF=true`
- ถ้าจะใช้ LINE จริง ต้องตั้ง `LINE_LIFF_CHANNEL_ID` และ `VITE_LIFF_ID` ให้ตรงกัน และปิด mock ทั้งสองฝั่งพร้อมกัน

### **Protected photo storage**
- รูปนักเรียนและรูปผู้ปกครองถูกอัปโหลดผ่าน `multipart/form-data` ไปยัง backend เท่านั้น
- backend เก็บไฟล์ไว้ใน `CsrApi/App_Data/ProtectedUploads` และ **ไม่** เปิดเป็น public static files
- การเรียกดูรูปใช้ endpoint ที่ต้องมี LINE access token:
  - `GET /api/me/student-photo`
  - `GET /api/me/guardian-photo`
- ตั้งค่าได้ผ่าน `PhotoStorage` ใน `CsrApi/appsettings.json`

### **Seed / test data**
- development compose จะ seed ข้อมูลทดสอบให้เองทุกครั้งแบบ idempotent
- มีข้อมูลผู้ปกครอง mock และนักเรียนตัวอย่างสำหรับทดสอบหน้า `Dashboard` และ `ClassList`
- ถ้าต้องการเริ่ม dev data ใหม่ ให้ลบ volume แล้วรันใหม่

```bash
docker compose -f docker-compose.dev.yml down -v
docker compose -f docker-compose.dev.yml up --build
```

### **Stop services**
```bash
docker compose down
```

```bash
docker compose -f docker-compose.dev.yml down
```

### **Data persistence**
- production-like compose และ development compose ใช้ Docker volume สำหรับเก็บฐานข้อมูล
- การ `docker compose down` จะไม่ลบข้อมูลใน volume นั้น

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
| `PhotoFileName` | TEXT | - | ชื่อไฟล์รูปที่เก็บใน protected storage |
| `PhotoContentType` | TEXT | - | MIME type ของรูป |
| `PhotoUploadedAtUtc` | TEXT | - | เวลาอัปโหลดรูป |
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
| `PhotoFileName` | TEXT | - | ชื่อไฟล์รูปที่เก็บใน protected storage |
| `PhotoContentType` | TEXT | - | MIME type ของรูป |
| `PhotoUploadedAtUtc` | TEXT | - | เวลาอัปโหลดรูป |

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
   - ชื่อจริงจะแสดงเป็น: `สมชาย ใจ**`
   - เบอร์โทรจะแสดงเป็น: `081-XXX-5678`
3. **Protected Media Access:** รูปทั้งหมดถูกเก็บนอก public static serving และเข้าถึงได้เฉพาะผ่าน API ที่ยืนยัน LINE token แล้ว
4. **Identity Verification:** ผู้ปกครองต้องกรอก `StudentId` + `OldRoom` + `OldNo` เพื่อทำการ Binding LINE ID ในครั้งแรก

---

## 📱 4. Frontend Architecture (Vue.js 3 + Tailwind)

### **Key Screens**
- **Auth:** `Login.vue` (LIFF Integration) & `Consent.vue` (PDPA Agreement)
- **Home:** `Dashboard.vue` (สรุปสถานะนักเรียนรายบุคคล)
- **Directory:** `ClassList.vue` (รายชื่อเพื่อนในห้องแบบ Masked Name)
- **Contacts:** `CommitteeList.vue` (เบอร์ติดต่อครูและกรรมการ พร้อมปุ่ม Click-to-Call)

### **Frontend Logic**
- หน้า frontend เรียก API ผ่าน `/api/*`
- หน้า `Register.vue` ทำหน้าที่ orchestration เท่านั้น โดยแยก form state ไปที่ `useRegistrationForm` และแยก UI รูปเป็น `StudentPhotoUpload.vue` กับ `GuardianPhotoUpload.vue`
- ใน production-like compose, frontend static files ถูกเสิร์ฟผ่าน `nginx` และ proxy `/api` ไปที่ backend
- ใน development compose, Vite dev server จะ proxy `/api` ไปที่ backend service ภายใน Docker network