# 🧪 Phase 15: End-to-End Test Plan for User Scenarios
**Status:** Implemented
**Depends on:** existing front-office flows, backoffice routes, development compose with mock auth + seeded data
**Goal:** วางแผนและเตรียมชุด end-to-end (E2E) tests ที่ทดสอบ user scenarios สำคัญของระบบแบบใช้งานจริงตั้งแต่หน้า UI จนถึง API integration เพื่อให้มั่นใจว่า flow หลักไม่พังเมื่อมีการแก้ไขโค้ดในอนาคต

---

## 🎯 1. Problem Statement
ตอนนี้ระบบมี feature หลักค่อนข้างครบแล้ว แต่ยังมีช่องโหว่ชัดเจนเรื่อง **regression safety**:

- flow หลักหลายจุดพึ่งการทดสอบด้วยมือ
- การเปลี่ยนหน้า, validation, upload, role-based UI, และ API integration มีโอกาสพังข้ามกันได้ง่าย
- งานหลาย phase ก่อนหน้าเพิ่ม behavior สำคัญ เช่น required fields, protected photo access, document view, navigation, และ backoffice role handling แต่ยังไม่มี test suite ที่รันตรวจ flow แบบ end-to-end
- ถ้ายังไม่มี E2E tests ระบบจะดูเหมือนครบ แต่ความมั่นใจเวลาปรับ refactor หรือเพิ่ม feature ใหม่จะต่ำมาก

สรุปตรง ๆ:
- ถ้าระบบเริ่มเข้าสู่ช่วง hardening แล้ว แต่ยังไม่มี user-scenario E2E tests เลย ถือว่ายังเสี่ยง
- unit test อย่างเดียวไม่พอ เพราะงานนี้มีหลายจุดที่พังจาก integration ระหว่าง frontend, router, auth mock, backend, database seed, และ file upload

---

## 🔍 2. Current Findings From Codebase

### **A: มี development stack ที่เหมาะกับ E2E อยู่แล้ว**
จาก `README.md`:
- มี `docker-compose.dev.yml`
- มี mock auth ทั้ง frontend/backend
- มี seeded data แบบ idempotent

นี่เป็นฐานที่ดีมากสำหรับ E2E เพราะทำให้ test environment คาดเดาได้และลด dependency กับ LINE จริง

### **B: Front-office routes หลักค่อนข้างชัดแล้ว**
จาก `csr-frontend/src/router/index.js`:
- `/dashboard`
- `/class-list`
- `/contacts`
- `/register`
- `/profile/edit`
- `/document`

จึงสามารถออกแบบ scenario test ตาม user journey จริงได้ค่อนข้างตรง

### **C: Backoffice routes มีแล้ว แต่ไม่ควรยัดทั้งหมดเข้ารอบแรก**
มี route เช่น:
- `/backoffice/dashboard`
- `/backoffice/students`
- `/backoffice/students/:id`
- `/backoffice/staff`

แต่ฝั่ง backoffice ยังเป็น area ที่ authz และ business rules สำคัญกว่า front-office ดังนั้นรอบแรกควรเลือกเฉพาะ scenario ที่ stable และคุ้มต่อ regression จริง

### **D: ตอนนี้ frontend ยังไม่มี test framework สำหรับ E2E**
จาก `csr-frontend/package.json`:
- ยังไม่มี Playwright หรือ Cypress
- script ปัจจุบันมีแค่ `dev`, `build`, `preview`

แปลว่า task นี้ต้องครอบคลุมทั้ง:
- การเลือก framework
- การตั้งโครงสร้าง test
- การกำหนด execution flow
- การจัดการ artifacts/report

---

## 💥 3. Root Cause
ปัญหาจริงไม่ใช่แค่ “ยังไม่ได้เขียน test” แต่คือ:

- ระบบโตขึ้นเร็วกว่าระบบทดสอบ
- acceptance หลายข้อถูก verify ด้วยสายตาหรือ manual click-through เป็นหลัก
- ไม่มี regression suite กลางที่บอกได้ชัดว่า flow ผู้ใช้หลักยังรอดหลังเปลี่ยน code

ดังนั้นงานนี้ต้องแก้ที่ **test architecture และ scenario selection** ไม่ใช่แค่เพิ่ม test case สุ่ม ๆ

---

## 🧭 4. Testing Strategy Principles

### **A: Test user scenarios, not implementation details**
E2E ต้องยึด behavior ที่ผู้ใช้ทำจริง เช่น:
- เปิด dashboard
- เข้าไปแก้ไขข้อมูล
- submit form
- ดูเอกสาร
- เปิดรายชื่อเพื่อน

ไม่ควร test coupling กับ DOM ภายในที่เปลี่ยนง่ายโดยไม่กระทบ user behavior

### **B: Start with deterministic environment**
รอบแรกควรใช้:
- mock LIFF
- mock backend auth
- seeded data
- local/dev compose environment

ไม่ควรเริ่มด้วย LINE login จริง เพราะจะทำให้ test เปราะและ debug ยาก

### **C: Cover the highest-value flows first**
ลำดับความสำคัญควรเป็น:
1. front-office critical happy path
2. front-office validation/error path
3. navigation regression checks
4. backoffice smoke coverage
5. role/permission coverage ที่คุ้มและเสถียร

### **D: Keep tests maintainable**
- แยก reusable helpers สำหรับ login/setup/navigation
- ใช้ page object หรือ helper layer แบบไม่หนักเกินไป
- หลีกเลี่ยง test ที่ยาวเกินและพยายามเช็กหลายเรื่องในเคสเดียว

### **E: Do not make real external dependencies a hard requirement**
first pass ห้ามผูกกับ:
- LINE real auth
- external network behavior ที่ควบคุมไม่ได้
- manual preconditions ที่ทำซ้ำยาก

---

## 🧩 5. Recommended Framework Choice

### **Recommendation: Playwright**
สำหรับโปรเจกต์นี้ Playwright เหมาะกว่า Cypress ในรอบแรก เพราะ:

- รองรับ modern browser automation ดี
- เหมาะกับ multi-page / auth / file upload / route navigation
- มี trace, screenshot, video, HTML report พร้อมใช้งาน
- ใช้งานกับ CI ได้ดีในอนาคต
- เหมาะกับการทดสอบ Vue + Vite app โดยไม่ต้อง hack เยอะ

### **What this task should introduce**
- `@playwright/test`
- config กลางสำหรับ base URL และ web server strategy
- โฟลเดอร์ test เช่น `csr-frontend/tests/e2e/`
- helper สำหรับ mock-auth session หรือ deterministic startup
- report/trace configuration

---

## 🧱 6. MVP Scope of Work (First Pass Only)

### **A: E2E infrastructure that is just enough to run stable tests**
สิ่งที่ต้องเพิ่มในรอบแรก:
- ติดตั้ง Playwright และ browser dependencies ตามแนวทางมาตรฐาน
- เพิ่ม npm scripts สำหรับ run tests
- เพิ่ม Playwright config
- กำหนด output/report directory
- วางโครงสร้างโฟลเดอร์ test ให้ชัดเจน

ข้อกำหนดสำคัญ:
- ห้ามออกแบบ infra เผื่อทุกกรณีจนซับซ้อนเกินจำเป็น
- suite แรกต้องรันง่ายบน dev environment ที่ใช้ mock auth + seeded data

### **B: MVP scenarios = 6 high-value front-office scenarios เท่านั้น**
รอบแรกให้จำกัดแค่ 6 scenarios นี้ก่อน:
1. **Dashboard loads successfully**
- ผู้ใช้เข้า `/dashboard`
- เห็นข้อมูลหลักจาก seeded/mock data
- ไม่มี fatal error state
2. **Class list loads successfully**
- ผู้ใช้เข้า `/class-list`
- เห็นรายการเพื่อนในห้องหรือ content หลัก
- ไม่มี error state ผิดปกติ
3. **Contacts screen loads successfully**
- ผู้ใช้เข้า `/contacts`
- เห็นข้อมูลติดต่อหลัก
- ไม่มี error state ผิดปกติ
4. **Navigation to edit/document and back home works**
- ผู้ใช้เปิด `/profile/edit` หรือ `/document`
- ปุ่ม `Back` / `Home` ทำงานตาม expected behavior
- กลับสู่ `/dashboard` ได้จริง
5. **Registration validation blocks invalid submit**
- เปิดหน้า `Register` หรือ `EditProfile`
- ลอง submit โดยปล่อย required fields ว่าง
- เห็น error message ที่ field สำคัญ เช่น `Student ID`, `เลขที่`
- request ไม่ควรผ่านแบบปลอม ๆ
6. **Registration submit succeeds and persists data**
- กรอก/แก้ข้อมูลสำคัญให้ครบ
- submit สำเร็จ
- เห็น success state หรือกลับสู่ flow ที่ถูกต้อง
- reload แล้วข้อมูลยังสะท้อนค่าที่บันทึก

### **C: Explicitly deferred from MVP first pass**
ของต่อไปนี้ **ยังไม่ทำในรอบแรก**:
- file upload scenarios
- backoffice smoke tests
- authorization-focused scenarios ของ staff roles
- deeper negative cases ที่ต้องใช้ fixture พิเศษจำนวนมาก

เหตุผล:
- ทั้งหมดนี้เพิ่ม maintenance cost และความเปราะได้เร็วเกินไป
- ยังไม่คุ้มก่อนที่ front-office regression suite แรกจะนิ่งจริง

---

## 🗂️ 7. Proposed MVP Test Structure

### **Directory structure**
```text
csr-frontend/
  tests/
    e2e/
      frontoffice/
        dashboard.spec.js
        directory.spec.js
        navigation.spec.js
        registration.spec.js
      helpers/
        auth.js
        routes.js
        assertions.js
  playwright.config.js
```

### **Naming principles**
- ตั้งชื่อตาม scenario ไม่ใช่ชื่อ component
- สำหรับ MVP ให้โฟกัสเฉพาะ `frontoffice`
- helper ต้องเล็กและ reusable

---

## ⚙️ 8. Suggested MVP Implementation Plan

### **Step 1: Add minimal Playwright infrastructure**
Target files ที่น่าจะต้องแตะ:
- `csr-frontend/package.json`
- `csr-frontend/playwright.config.js` หรือ `.ts`
- `csr-frontend/tests/e2e/**`
- `.gitignore` (ถ้ามี report/trace/video artifacts ใหม่)
- `README.md`

สิ่งที่ต้องทำ:
- เพิ่ม dependency และ scripts เช่น `test:e2e`, `test:e2e:ui`, `test:e2e:headed`
- กำหนด base URL ให้ตรงกับ environment dev ที่ใช้จริง
- ตัดสินใจว่าจะให้ Playwright start web server เอง หรือให้ run กับ docker dev stack ที่เปิดไว้แล้ว

### **Step 2: Add 4 front-office smoke tests**
เริ่มจาก scenario ที่ไม่เปราะที่สุด:
- dashboard
- class list
- contacts
- document/navigation back-home

### **Step 3: Add 2 registration tests**
- invalid submit shows validation
- valid submit succeeds and persists after reload

### **Step 4: Add docs only for MVP usage**
- README บอกวิธีรัน E2E ชุดแรก
- ระบุ preconditions ให้ชัด
- ยังไม่ต่อ CI และยังไม่ขยายไป backoffice ใน task นี้

---

## ✅ 9. MVP Implementation Checklist

### **Infrastructure**
- [x] ติดตั้ง `@playwright/test`
- [x] สร้าง Playwright config
- [x] เพิ่ม scripts ใน `package.json`
- [x] กำหนด browser/project strategy ที่เหมาะสม
- [x] ตั้งค่า screenshot/trace/video policy สำหรับ failed tests
- [x] สร้างโฟลเดอร์ `tests/e2e`

### **Environment / reliability**
- [x] กำหนดวิธีใช้ mock auth ให้ deterministic
- [x] ยืนยันว่า dev seed data เพียงพอสำหรับ scenario หลัก
- [x] หลีกเลี่ยง dependency กับ LINE real login ในรอบแรก

### **MVP scenarios**
- [x] dashboard load
- [x] class list load
- [x] contacts load
- [x] document render + navigation back/home
- [x] invalid registration submit shows validation
- [x] valid registration submit succeeds
- [x] reload after save reflects persisted data

### **Deferred after MVP**
- [ ] student or guardian photo upload happy path
- [ ] teacher dashboard smoke test
- [ ] teacher student list smoke test
- [ ] teacher student detail smoke test
- [ ] unauthorized user blocked from backoffice
- [ ] read-only role does not expose forbidden write actions

### **Documentation**
- [x] อัปเดต `README.md` วิธีรัน E2E tests
- [x] อธิบายว่า suite แรกใช้ mock auth / seeded data
- [x] ระบุสิ่งที่ยัง deferred เช่น upload/backoffice/authz scenarios

---

## 🧪 10. MVP Acceptance Criteria
- สามารถรัน E2E suite พื้นฐานบนเครื่อง dev ได้แบบซ้ำได้ผลใกล้เคียงเดิม
- มีชุด front-office smoke tests สำหรับ `dashboard`, `class-list`, `contacts`, และ `document/navigation`
- มี registration tests ที่ครอบคลุม invalid + valid flow
- มีอย่างน้อย 1 เคสที่ยืนยันว่าข้อมูลหลัง save ยังอยู่หลัง reload
- เมื่อ test fail ต้องมี artifact ที่ช่วย debug ได้ เช่น trace/screenshot/report
- README อธิบายวิธีรัน test และ preconditions ได้ชัดเจน
- suite แรกไม่พึ่ง LINE real login, file upload flow, หรือ backoffice roles

---

## 🚫 11. Out of Scope for MVP First Pass
- backoffice smoke tests
- staff authorization scenarios
- file upload scenarios
- LINE login จริงผ่าน production LIFF flow
- cross-browser matrix ใหญ่เกินจำเป็นทุกตัวตั้งแต่วันแรก
- visual regression testing เต็มรูปแบบ
- performance/load testing
- mobile device farm integration
- PDF binary comparison แบบละเอียด
- full permission matrix ทุก role/ทุก endpoint

---

## ⚠️ 12. Risks / Notes
- ถ้าใช้ seeded data ที่ไม่ deterministic พอ test จะ flaky ทันที
- ถ้า selector อิง text หรือ DOM structure แบบเปราะเกินไป maintenance cost จะสูง
- ถ้า suite พยายามครอบคลุมทุก flow ในรอบแรก งานจะบานและได้ test ที่เปราะ
- ถ้าไปผูกกับ LINE auth จริงตั้งแต่ต้น จะเสียเวลาที่ infrastructure มากกว่าการป้องกัน regression จริง
- ถ้า backoffice role data ยังไม่นิ่ง ควรแยก smoke tests ออกจาก deeper authorization scenarios
- ถ้ามี upload test ต้องระวัง cleanup และขนาดไฟล์ให้ deterministic

---

## 🧭 13. Recommended MVP Delivery Order
1. Playwright setup + README + basic smoke wiring
2. Dashboard, class-list, contacts smoke tests
3. Document + navigation back/home test
4. Registration invalid submit test
5. Registration valid submit + reload persistence test
6. Upload, backoffice, authz, และ CI integration ในรอบถัดไป

---

## 🧠 14. Honest Recommendation
อย่าเริ่มจากเคสเยอะ

เริ่มจาก **6 scenarios นี้เท่านั้น** ก่อน:
1. dashboard
2. class list
3. contacts
4. document + navigation back/home
5. registration invalid submit
6. registration valid submit + reload persistence

ใช้ mock auth + seeded data ให้สุด

บีบ suite แรกให้ **เสถียรและน่าเชื่อถือ** ก่อน แล้วค่อยขยาย

ถ้าทำถูก งานนี้จะเป็นตัวเปลี่ยนสถานะโปรเจกต์จาก “feature เยอะ” ไปเป็น “แก้ต่อได้อย่างมั่นใจ”
