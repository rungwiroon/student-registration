# 🚀 Phase 3: Live Data API Integration & PDPA Masking
**Current Status:** Completed
**Next Focus:** เดินหน้าต่อที่ phase ถัดไป เช่น secure photo upload, image sniffing และ frontend validation

---

## 🔒 1. Server-Side Masking Strategy (PDPA)
ข้อมูลเพื่อนในห้องเรียน (Class List) มีความอ่อนไหว:
*   ❌ **ไม่ควร:** ดึงข้อมูลเต็มส่งไปยัง Frontend แล้วค่อยเบลอ เพราะมีโอกาสที่ผู้ปกครองบางคนเปิด F12 ดูข้อมูลดิบของเด็กคนอื่น
*   ✅ **ต้องทำ:** ปิดบังข้อมูล (Masking) ให้เสร็จสิ้นระดับ Backend C# แล้วส่งแต่ข้อมูลที่เบลอแล้วให้ Frontend เท่านั้น

---

## ⚙️ 2. Backend: New API Endpoints

### **A: `GET /api/me` (เซสชันปัจจุบัน)**
สำหรับหน้า Dashboard แสดงข้อมูลของตัวเองแบบเต็ม เพื่อตรวจสอบว่าข้อมูลลงทะเบียนถูกต้องไหม
*   **Request:** รับ Token (LIFF) ดึงเอา `LineUserId`
*   **Logic:**
    1. ค้นหาผู้ปกครอง (Guardian) ด้วย `LineUserId`
    2. ค้นหานักเรียนของเข็มนั้น (Student) 
    3. ใช้ `EncryptionService` ถอดรหัสชื่อ เบอร์โทร และเบอร์เด็ก
*   **Response:** ห่อใส่ JSON DTO ปล่อยออกแบบ Unmasked (เพราะเป็นเจ้าของข้อมูล)

### **B: `GET /api/class` (สมุดรายชื่อ PDPA)**
สำหรับหน้า ClassList โชว์นักเรียนทั้งห้อง
*   **Request:** `GET`
*   **Logic:**
    1. ดึงนักเรียนห้อง "ม.1/2"
    2. ใช้ `EncryptionService` ถอดรหัส แล้วค่อยเข้าฟังก์ชัน `MaskingService` เพื่อปิดนามสกุลและเบอร์โทร
*   **Response:** ปล่อยออกรายการ Array ของ DTO ที่เป็นตัวอักษร "ด.ช. พัสรก ใ*******"

---

## 📱 3. Frontend: Logic Wiring
*   ปรับ `Dashboard.vue` ให้ Loading ลากข้อมูลจาก `/api/me` ถ้าเกิด error/401 ให้เด้งกลับไปที่ฟอร์ม `/register`
*   ปรับ `ClassList.vue` ลบ `mockStudents` ฝังโค้ดทิ้ง เปลี่ยนเป็น `fetch` ขอข้อมูลจาก `/api/class` แทน
