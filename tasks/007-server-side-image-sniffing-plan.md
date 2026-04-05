# 🛡️ Phase 5: Server-Side Image Content & Dimension Validation
**Status:** Planned
**Goal:** เพิ่มการตรวจสอบไฟล์รูปภาพฝั่ง server ให้เข้มขึ้น โดย backend ต้องยืนยันให้ได้ว่าไฟล์ที่อัปโหลดเป็นรูปจริง ขนาดภาพสมเหตุสมผล และไม่เชื่อแค่ filename หรือ `Content-Type` จาก client

---

## 🎯 1. Problem Statement
ปัจจุบันระบบ secure photo upload รองรับการตรวจประเภทไฟล์และขนาดไฟล์ระดับพื้นฐานแล้ว แต่ถ้าตรวจแค่ extension หรือ header จาก request อย่างเดียว ยังไม่พอสำหรับงานที่เกี่ยวกับข้อมูลส่วนบุคคลของนักเรียนและผู้ปกครอง

ความเสี่ยงที่ยังต้องปิดเพิ่ม:
- อัปโหลดไฟล์ปลอมที่เปลี่ยนนามสกุลเป็น `.jpg` หรือ `.png`
- ส่ง `Content-Type` ปลอม เช่น `image/jpeg` แต่เนื้อไฟล์จริงไม่ใช่รูป
- อัปโหลดรูปที่มีขนาดมิติเกินจริง เช่น width/height ใหญ่มากผิดปกติ จนกิน memory หรือทำให้ระบบประมวลผลหนักเกินจำเป็น
- อัปโหลดรูปที่เล็กเกินไปจนใช้งานจริงไม่ได้

---

## 🔍 2. Scope of Work

### **A: Content sniffing (must-do)**
backend ต้องตรวจ binary signature หรือ decode ไฟล์จริงก่อนบันทึกลง protected storage

สิ่งที่ต้อง enforce:
- อนุญาตเฉพาะ JPEG, PNG, WEBP ที่เป็นไฟล์รูปจริง
- ถ้า signature ไม่ตรงกับชนิดไฟล์ที่อ้างมา ให้ reject ทันที
- ถ้า decode ภาพไม่ได้ ให้ถือว่าเป็น invalid upload

### **B: Dimension sniffing (must-do)**
backend ต้องอ่าน metadata หรือขนาดจริงของภาพก่อนบันทึก

สิ่งที่ควรกำหนด:
- minimum width / height
- maximum width / height
- maximum total pixels
- optional: บังคับ aspect ratio สำหรับรูป profile ถ้าทีมต้องการ

### **C: Error handling**
เมื่อ validation ไม่ผ่าน ต้องตอบกลับด้วย error message ที่ชัดเจน เช่น
- `Unsupported image format.`
- `Image dimensions are too large.`
- `Image dimensions are too small.`
- `Image content does not match the declared content type.`

---

## ⚙️ 3. Backend Implementation Plan (.NET)

### **Target files**
- `CsrApi/Services/PhotoStorageService.cs`
- `CsrApi/Program.cs`
- `CsrApi/appsettings.json`
- ถ้าจำเป็นค่อยเพิ่ม options/model ใหม่สำหรับ image validation settings

### **Expected design**
- ขยาย `PhotoStorageOptions` ให้รองรับ config เช่น:
  - `MinWidth`
  - `MinHeight`
  - `MaxWidth`
  - `MaxHeight`
  - `MaxTotalPixels`
- ใน `PhotoStorageService.SaveAsync(...)`
  - อ่าน stream อย่างปลอดภัย
  - sniff signature จาก content จริง
  - decode image metadata เพื่อตรวจมิติ
  - reject ก่อนเขียนลง disk ถ้า validation ไม่ผ่าน
- ต้อง reset stream position ให้ถูกต้อง หากมีการอ่านหลายรอบ
- หลีกเลี่ยงการเขียน method ยาวเกินไป ควรแยก function ตาม responsibility เช่น
  - `ValidateContentType`
  - `DetectImageFormat`
  - `ValidateImageDimensions`
  - `CreateValidationError`

---

## 🔒 4. Security Rules
- ห้ามเชื่อ `file.FileName` จาก client ว่าเป็นข้อพิสูจน์ชนิดไฟล์
- ห้ามเชื่อ `file.ContentType` เพียงอย่างเดียว
- ต้อง validate ก่อนบันทึกเข้า `App_Data/ProtectedUploads`
- ยังคงไม่ใช้ public static serving
- error response ต้องไม่เปิดเผย path ภายในเครื่องหรือข้อมูล implementation ที่มากเกินจำเป็น

---

## 🧪 5. Acceptance Criteria
- อัปโหลด JPEG/PNG/WEBP ปกติได้สำเร็จ
- ไฟล์ที่ปลอม extension ถูก reject
- ไฟล์ที่ `Content-Type` ไม่ตรงกับ content จริงถูก reject
- ไฟล์รูปที่ dimension ใหญ่เกิน config ถูก reject
- ไฟล์รูปที่ dimension เล็กเกิน config ถูก reject
- backend ยัง build ผ่าน และ registration flow เดิมยังไม่พัง
- protected photo endpoints เดิมยังทำงานได้ตามปกติ

---

## 🧭 6. Nice-to-have (not required in this task)
- client-side compression ก่อน upload เพื่อลด bandwidth
- normalize หรือ resize รูปฝั่ง server หลัง validation
- strip EXIF metadata เพื่อลดข้อมูลส่วนเกิน
- integration tests สำหรับ malicious upload cases
