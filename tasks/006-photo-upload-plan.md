# 📸 Phase 4: Photo Upload Feature
**Status:** Planned (To be implemented after API Integration)
**Goal:** พัฒนาระบบอัปโหลดรูปภาพประจำตัวนักเรียน (Profile Picture) หรือเอกสารยืนยันตัวตน เพื่อจัดเก็บได้อย่างปลอดภัย

---

## 📱 1. Frontend: Upload UI (`Vue 3`)
- เพิ่มส่วน "อัปโหลดรูปภาพนักเรียน" ในหน้า `Register.vue` (หรือเปิดเป็นแท็บเมนูใหม่สำหรับการอัปเดตรูปภาพโดยเฉพาะ)
- **Image Preview:** ผู้ใช้เลือกรูปจากมือถือ (หรือถ่ายรูปใหม่) แล้วแสดงตัวอย่าง (Preview) รูปทันทีก่อนอัปโหลด
- **Client-Side Image Compression:** หากไฟล์จากกล้องมือถือมีขนาดใหญ่หลาย MB, ระบบจะทำการบีบอัดรูปภาพด้วย JavaScript (เช่น ใช้ Canvas หรือ Library) ให้เหลือขนาดเล็ก (เช่น ไม่เกิน 500KB) เพื่อลดภาระเซิร์ฟเวอร์

## ⚙️ 2. Backend: API Endpoint (`.NET 10`)
- **Endpoint:** `POST /api/upload` (รับข้อมูลแบบ `multipart/form-data`)
- **Security Check:** ตรวจสอบนามสกุลไฟล์อนุญาตเฉพาะ `.jpg`, `.jpeg`, `.png`, และป้องกันอัปโหลดไฟล์ที่มีขนาดใหญ่เกินไป
- **Storage Strategy:**
  - สร้างแฟ้มจัดเก็บในสถาปัตยกรรมของเซิร์ฟเวอร์ (เช่น โฟลเดอร์ `wwwroot/uploads/students`)
  - สร้างชื่อไฟล์ใหม่เพื่อป้องกันชื่อซ้ำและกันการคาดเดาจากภายนอก (เช่น ใช้ `Guid.NewGuid()`)
- **Database Update:** นำชื่อ Path รูปภาพที่เซฟเสร็จแล้ว ไปบันทึกลงคอลัมน์ใหม่ในตาราง `Students` (เช่น `ProfilePhotoPath`)

## 🗄️ 3. Database Schema Evolution
- อัปเดตโครงสร้าง Table `Students` โดยเพิ่มตัวแปร (Column):
  - `ProfileImagePath VARCHAR nullable`

## 🔒 4. PDPA & Security
- รูปภาพเอกสารต่างๆ ถือว่าเป็นข้อมูลส่วนบุคคล การอนุญาตให้กดดูรูปภาพในระบบหลังบ้าน (Dashboard ฝั่งแอดมิน) จำเป็นต้องมี Token ตรวจสอบสิทธิผู้เข้าดู (authorization scheme) ป้องกันไม่ให้บุคคลภายนอกนำ URL มาเปิดดูรูปภาพเด็กได้ตามอำเภอใจ
