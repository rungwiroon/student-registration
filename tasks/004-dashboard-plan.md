# 🚀 Phase 2: Dashboard, Directory UI & API Integration
**Current Status:** Registration API (`/api/register`) & Form UI (`Register.vue`) completely finished!
**Next Focus:** Main Application Views (Dashboard, Class List) with Vue Router and Bottom Navigation.

---

## 📱 1. Frontend: Main Application Views

### **A. Layout: MainLayout & Bottom Navigation**
สร้างโครงสร้างหลักสำหรับหน้าแอปพลิเคชันหลัง Login/ลงทะเบียน สำเร็จ:
*   มี `BottomNav.vue` สำหรับสลับหน้าจอ (Tab 1: หน้าแรก, Tab 2: เพื่อนในห้อง, Tab 3: ติดต่อ)
*   ครอบทุก View ในแอปด้วย Layout นี้

### **B. View: Dashboard (`Dashboard.vue`)**
*   **Hero Section:** แสดงชื่อเล่นนักเรียน, รหัสประจำตัว (ถ้ามี), และ "ห้อง ม.1/2 เลขที่ X"
*   **Quick Info Card:** แสดงเบอร์โทรที่บันทึกไว้ และสถานะข้อมูล (เช่น "อัปเดตข้อมูลล่าสุดเมื่อ...")
*   **Action Button:** ปุ่ม "แก้ไขข้อมูล" (พาไปที่หน้า Register ฟอร์มเก่าเพื่ออัปเดตข้อมูล)

### **C. View: Class Directory (`ClassList.vue`)**
*   **Data List:** แสดงรายชื่อเพื่อนในห้องแบบ List เรียงตามเลขที่
*   **PDPA Masking Requirement:** นำ `maskFullName` utility มาครอบชื่อจริงก่อน render เสมอ (เช่น ด.ช. สมชาย ใ*******)

### **D. View: Contacts (`Contacts.vue`)**
*   หน้ารวมเบอร์ติดต่อ ครูที่ปรึกษา และกรรมการห้อง (Click-to-call)

---

## ⚙️ 2. Backend: API Endpoints (.NET 10)

~~*Endpoint: `POST /api/register` สำหรับลงทะเบียน*~~ **(✅ เสร็จเรียบร้อยแล้วใน Phase ก่อนหน้า!)**

**ส่วนที่เตรียมพัฒนาถัดไป:**
*   `GET /api/students` หรือ `GET /api/class` สำหรับดึงรายชื่อเพื่อนๆ ม.1/2 เพื่อมาแสดงในหน้าตารางสมุดรายชื่อ (นำ Masking มาช่วยบังทั้งหน้าบ้านและหลังบ้าน)

---

## 🤖 3. Prompts for Antigravity AI (Copy & Paste)

**Prompt 1: Layout & Dashboard (Frontend)**
"Create a MainLayout.vue with a fixed Bottom Navigation bar (Home, Class List, Contacts) using Tailwind CSS. Then, create a Dashboard.vue component that displays the logged-in student's info (mock data: Student Name, Room 1/2, Number, and Parent Info). Set up Vue Router to use this layout. Ensure it fits well on a mobile screen (max-width 480px)."

**Prompt 2: Class Directory with Masking (Frontend)**
"Create a ClassList.vue component. Generate a mock array of 10 students for Room 1/2. Use Tailwind to render a clean list. Before rendering each student's name, apply the imported masking function that shows only the first name and the first letter of the last name followed by asterisks."