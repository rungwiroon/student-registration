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
  - `GET /api/me/guardian-photo/{guardianOrder}` (ระบุลำดับผู้ปกครอง 1 หรือ 2)
- ตั้งค่าได้ผ่าน `PhotoStorage` ใน `CsrApi/appsettings.json`

### **Introduction Document**
- หน้า `/document` แสดงใบแนะนำตัวนักเรียนและผู้ปกครอง
- Endpoint: `GET /api/me/introduction-document`
- รองรับการพิมพ์และบันทึกเป็น PDF ผ่าน browser print function

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
| `StudentId` | TEXT | - | รหัสประจำตัว (เช่น 30558) — **required in registration flow**, uniqueness enforced at application layer (DB nullable, target: unique constraint) |
| `OldRoom` | TEXT | - | ห้องเดิมตอน ม.1 |
| `OldNo` | INTEGER | - | เลขที่เดิม |
| `NewRoom` | TEXT | - | ห้องใหม่ (ม.1/2) |
| `NewNo` | INTEGER | - | เลขที่ใหม่ — **required in registration flow** (DB nullable for legacy data) |
| `EncName` | TEXT | NOT NULL | ชื่อ-นามสกุลจริง (**AES-256**) (legacy) |
| `EncFirstName` | TEXT | - | ชื่อจริง (**AES-256**) |
| `EncLastName` | TEXT | - | นามสกุล (**AES-256**) |
| `Nickname` | TEXT | - | ชื่อเล่น |
| `EncPhone` | TEXT | - | เบอร์โทรศัพท์ (**AES-256**) |
| `BloodType` | TEXT | - | กรุ๊ปเลือด |
| `DOB` | TEXT | - | วันเกิด (format: `yyyy-MM-dd`) |
| `PhotoFileName` | TEXT | - | ชื่อไฟล์รูปที่เก็บใน protected storage |
| `PhotoContentType` | TEXT | - | MIME type ของรูป |
| `PhotoUploadedAtUtc` | TEXT | - | เวลาอัปโหลดรูป |
| `Status` | TEXT | - | สถานะการเรียน (ปกติ/ร/มผ) |

### **Table: `Guardians`**
| Column | Type | Constraints | Logic / Security |
| :--- | :--- | :--- | :--- |
| `Id` | GUID | PRIMARY KEY | - |
| `StudentId` | GUID | FK | เชื่อมกับ Students |
| `GuardianOrder` | INTEGER | - | ลำดับผู้ปกครอง (1 = หลัก, 2 = รอง) |
| `Relation` | TEXT | - | บิดา / มารดา / ผู้ปกครอง |
| `EncName` | TEXT | - | ชื่อ-นามสกุล (**AES-256**) (legacy) |
| `EncFirstName` | TEXT | - | ชื่อ (**AES-256**) |
| `EncLastName` | TEXT | - | นามสกุล (**AES-256**) |
| `EncPhone` | TEXT | - | เบอร์โทรศัพท์ (**AES-256**) |
| `Occupation` | TEXT | - | อาชีพ |
| `Email` | TEXT | - | อีเมล |
| `LineUserId` | TEXT | - | LINE ID จาก LIFF สำหรับ Login (เฉพาะ guardian หลัก) |
| `PhotoFileName` | TEXT | - | ชื่อไฟล์รูปที่เก็บใน protected storage |
| `PhotoContentType` | TEXT | - | MIME type ของรูป |
| `PhotoUploadedAtUtc` | TEXT | - | เวลาอัปโหลดรูป |

**Note:** ระบบรองรับผู้ปกครองได้สูงสุด 2 คนต่อนักเรียน โดย `GuardianOrder` ใช้ระบุลำดับ (1 = หลัก, 2 = รอง) และ `LineUserId` เชื่อมกับ guardian หลักเท่านั้น

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
- **Document:** `IntroductionDocumentView.vue` (ใบแนะนำตัวนักเรียนและผู้ปกครอง พร้อมปุ่มพิมพ์/บันทึก PDF)

### **Frontend Logic**
- หน้า frontend เรียก API ผ่าน `/api/*`
- หน้า `Register.vue` ทำหน้าที่ orchestration เท่านั้น โดยแยก form state ไปที่ `useRegistrationForm` และแยก UI รูปเป็น `StudentPhotoUpload.vue` กับ `GuardianPhotoUpload.vue`
- `Student ID` (รหัสประจำตัว) และ `เลขที่` เป็น field บังคับกรอกทั้งใน frontend และ backend validation แม้ว่า database schema ยังเป็น nullable เพื่อรองรับข้อมูลเดิม
- `StudentId` ต้องไม่ซ้ำกัน — uniqueness ถูกตรวจสอบที่ application layer ในรอบนี้ (target: unique constraint ใน DB รอบถัดไป)
- field `วันเกิด` ใช้ `@vuepic/vue-datepicker` แสดงผลเป็น `dd/MM/yyyy` และส่งค่าเข้า API เป็น `yyyy-MM-dd`
- ใน production-like compose, frontend static files ถูกเสิร์ฟผ่าน `nginx` และ proxy `/api` ไปที่ backend
- ใน development compose, Vite dev server จะ proxy `/api` ไปที่ backend service ภายใน Docker network

---

## 🧪 5. E2E Tests (Playwright)

### **Prerequisites**

- รัน dev stack ด้วย `docker compose -f docker-compose.dev.yml up --build`
- mock auth + seed data ต้องเปิดอยู่ (เป็น default ของ dev compose)

### **Run tests**

```bash
cd csr-frontend

# Run all E2E tests (headless)
npm run test:e2e

# Run with browser visible
npm run test:e2e:headed

# Run with Playwright UI
npm run test:e2e:ui
```

### **Current coverage (MVP)**

| Scenario | File |
| :--- | :--- |
| Dashboard loads seed data | `tests/e2e/frontoffice/dashboard.spec.js` |
| Class list loads classmates | `tests/e2e/frontoffice/directory.spec.js` |
| Contacts page shows info | `tests/e2e/frontoffice/directory.spec.js` |
| Document renders + Home navigation | `tests/e2e/frontoffice/navigation.spec.js` |
| Edit profile Back/Home navigation | `tests/e2e/frontoffice/navigation.spec.js` |
| Registration blocks invalid submit | `tests/e2e/frontoffice/registration.spec.js` |
| Registration valid submit + persist | `tests/e2e/frontoffice/registration.spec.js` |

### **Deferred**

- file upload scenarios
- backoffice smoke tests
- authorization/role scenarios
- CI integration
