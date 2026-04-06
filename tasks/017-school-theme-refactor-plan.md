# 🎨 Phase 17: School Theme Refactor Plan
**Status:** Proposed
**Depends on:** existing `csr-frontend` UI structure, `design/colors.png`, current Tailwind v4 setup in `csr-frontend/src/style.css`
**Goal:** วางแผนการปรับ theme ของเว็บให้สอดคล้องกับสีของโรงเรียน โดยยังคงความอ่านง่าย, usability, maintainability, และไม่ทำให้ UI ทั้งระบบซีดหรือ contrast พังจากการเอาสีอ่อนมาใช้ตรง ๆ

---

## 🎯 1. Problem Statement
ตอนนี้ระบบมีสี UI ที่ใช้งานได้อยู่แล้วในเชิง functional แต่ยังไม่ตรงกับภาพลักษณ์ของโรงเรียน

จาก `design/colors.png` สีหลักที่เห็นคือโทน **ชมพู + ฟ้า** และทั้งสองสีค่อนข้างอ่อน

ปัญหาคือ ถ้าทำแบบลวก ๆ โดยแทนสีเดิมทั้งระบบด้วยสีจากไฟล์นี้ตรง ๆ จะเกิดปัญหาหลายอย่างทันที:

- ปุ่มหลักไม่เด่นพอ
- ตัวหนังสือบนพื้นสีอ่านยาก
- active/hover/focus state ขาดน้ำหนัก
- layout โดยรวมดูจืดและไม่เหมือนระบบที่พร้อมใช้งานจริง
- backoffice ซึ่งควรอ่านง่ายและนิ่ง จะเสียความชัดเจนโดยไม่จำเป็น

สรุปตรง ๆ:
- งานนี้ **ไม่ใช่แค่เปลี่ยนสี 2 ค่า**
- งานนี้คือการทำ **theme foundation + semantic color system** ให้รองรับสีโรงเรียนอย่างถูกวิธี
- ถ้าไม่แก้โครงสร้างก่อน จะลงเอยด้วยการไล่เปลี่ยน class สีแบบ hardcode ไปเรื่อย ๆ และ maintain ยาก

---

## 🔍 2. Current Findings From Codebase

### **A: ตอนนี้มี design token กลางน้อยเกินไป**
จาก `csr-frontend/src/style.css`:
- มี `@theme`
- แต่กำหนดไว้แค่ `--color-primary` ตัวเดียว

แปลว่าโครงสร้างปัจจุบันยังไม่พอสำหรับ theme จริงจัง เพราะยังไม่มี token สำหรับ:
- text
- surface
- border
- muted state
- interactive state
- brand secondary
- accent

### **B: สีหลักของ UI ยัง hardcode กระจายอยู่หลายไฟล์**
จากการสำรวจ frontend พบว่ามีการใช้ class สีแบบตรง ๆ จำนวนมาก เช่น:
- `bg-emerald-*`
- `text-emerald-*`
- `bg-slate-*`
- `text-slate-*`
- `bg-amber-*`
- `bg-teal-*`
- `gray-*`

ผลคือ:
- การเปลี่ยน theme ไม่มีจุดศูนย์กลาง
- เปลี่ยนสีบางหน้าแล้วจะไม่สอดคล้องกับหน้าอื่น
- เสี่ยงเกิด UI drift ระหว่าง front-office กับ backoffice

### **C: Shared layout เป็นจุดเปลี่ยนภาพรวมของระบบ**
ไฟล์ที่ส่งผลต่อความรู้สึกของทั้งระบบชัดเจนคือ:
- `csr-frontend/src/style.css`
- `csr-frontend/src/App.vue`
- `csr-frontend/src/components/MainLayout.vue`
- `csr-frontend/src/components/BackofficeLayout.vue`
- shared component ที่มีปุ่ม/section/reusable block เช่น `PhotoUploadField.vue`

ดังนั้นถ้าจะทำให้คุ้ม ต้องเริ่มจากโครงสร้างร่วมก่อน ไม่ใช่ไล่เก็บทีละหน้าแบบไม่มีระบบ

### **D: สีโรงเรียนในไฟล์อ้างอิงค่อนข้างอ่อน**
จาก `design/colors.png`:
- มีชมพูสดแต่ค่อนข้างสว่าง
- มีฟ้าสดแต่ค่อนข้างสว่าง

นี่เหมาะกับการใช้เป็น:
- brand accent
- decorative highlight
- soft background
- section tint
- gradient

แต่ไม่เหมาะกับการใช้ตรง ๆ เป็น:
- สีปุ่มหลักทุกปุ่ม
- สีข้อความหลัก
- header background ทุกจุด
- active state ที่ต้องชัด

---

## 💥 3. Root Cause
ปัญหาจริงไม่ใช่แค่ว่า “ยังไม่ได้เปลี่ยนสีให้ตรงโรงเรียน” แต่คือ:

- ระบบยังไม่มี semantic theme layer ที่ชัด
- สีใน UI ผูกกับ utility classes โดยตรงมากเกินไป
- ยังไม่ได้แยก **brand color** ออกจาก **usable UI color**

ถ้าไม่แก้ตรงนี้ก่อน จะเกิดวงจรเดิม:
- เปลี่ยนสีแล้วไม่สวย
- เติม class สีเพิ่มเฉพาะจุด
- หน้าใหม่กับหน้าเก่าไม่เข้ากัน
- แก้รอบถัดไปยากขึ้นเรื่อย ๆ

---

## 🧭 4. Design Principles

### **A: Separate brand colors from semantic UI colors**
ต้องแยกให้ชัดว่า:
- `brand colors` = สีโรงเรียนที่ใช้สะท้อนอัตลักษณ์
- `semantic UI colors` = สีที่ใช้กับปุ่ม, พื้นหลัง, ข้อความ, border, focus, status

นี่คือหลักสำคัญที่สุดของงานนี้

### **B: Do not use soft brand colors directly for all interactive states**
สีอ่อนของโรงเรียนไม่ควรถูกใช้ตรง ๆ กับทุกอย่าง โดยเฉพาะ:
- primary button
- active nav
- important text
- hover/focus states

ควร derive สีเข้มขึ้นสำหรับ interactive usage

### **C: Keep backoffice more restrained than front-office**
front-office สามารถใช้ brand color ได้ชัดกว่า

แต่ backoffice ควรคุมโทนให้:
- อ่านง่าย
- professional
- ใช้งานกับ table/list/detail screen ได้ดี

พูดให้ชัดคือ อย่าเอาชมพูฟ้าอ่อนไปทา backoffice ทั้งระบบ มันจะดูเบาเกินไปและเสีย readability

### **D: Centralize theme decisions before page-level cleanup**
ต้องเริ่มจาก:
- theme tokens
- shared layout
- shared reusable components

แล้วค่อยไล่เก็บ page-level classes ตามหลัง

### **E: Maintain accessibility and contrast as a hard requirement**
งานนี้ห้ามเอาความ “ตรงสีแบรนด์” มาชนะความอ่านง่าย

ถ้าสีโรงเรียนอ่อนเกินไป ต้องปรับโทนสำหรับ usage จริง ไม่ใช่ฝืนใช้ตรง ๆ

---

## 🎨 5. Recommended Theme Strategy

### **A: Recommended palette model**
แนะนำให้แยก palette เป็น 3 ชั้น:

1. **Brand**
- `brand-primary` = ฟ้าของโรงเรียน
- `brand-secondary` = ชมพูของโรงเรียน

2. **Neutral UI**
- background หลัก
- text หลัก
- border
- card surface
- muted surface

3. **Interactive / derived brand**
- ปุ่มหลัก
- hover
- active
- focus ring
- selected state

### **B: Suggested usage approach**
ข้อแนะนำที่ pragmatic ที่สุด:
- ใช้ **ฟ้า** เป็นแกนหลักของ action/primary UI
- ใช้ **ชมพู** เป็น accent หรือ secondary highlight
- ใช้ neutral สีอ่อน/เข้มสำหรับ text, background, layout structure

### **C: Where the school colors should appear**
เหมาะกับการใช้ใน:
- top header accent
- icon accent
- hero/section accent
- badge
- selected chip/tab บางกรณี
- gradient หรือ soft background บาง section

### **D: Where the school colors should not dominate**
ไม่ควรใช้จนล้นใน:
- body text
- dense tables
- form labels
- border ทั้งระบบ
- warning/success/error colors

### **E: Status colors must remain independent**
สีสำหรับสถานะเช่น:
- success
- warning
- error
- info

ไม่ควรเอาไปผูกกับ palette โรงเรียน เพราะคนละหน้าที่กัน

---

## 🔒 6. Locked First-Pass Decisions

### **A: Brand direction for first pass is fixed**
เพื่อไม่ให้ implementation ตีความกันเอง รอบแรกให้ล็อกก่อนว่า:
- **ฟ้า** = `primary brand direction`
- **ชมพู** = `secondary accent direction`
- neutral colors ยังเป็นตัวคุม text, background, border, และ readability

### **B: Soft school colors will not be used directly as primary UI colors everywhere**
รอบแรกห้ามใช้สีอ่อนจาก reference ตรง ๆ กับ:
- primary button หลักของระบบทั้งหมด
- text หลักบนพื้นขาว
- active nav ทุกจุด
- backoffice shell หลัก

ถ้าจะใช้ ต้อง derive เฉดที่เข้มขึ้นสำหรับ interactive state

### **C: Front-office and backoffice will share the same language, but not the same intensity**
- front-office สามารถใช้ brand color ได้ชัดกว่า
- backoffice ต้องนิ่งกว่าและใช้ neutral เป็นฐานมากกว่า
- ห้ามทำให้ backoffice กลายเป็น pastel-led UI

### **D: Status colors stay independent**
สี success / warning / error / info ไม่ถูก rebrand ตามสีโรงเรียนในรอบแรก

### **E: First pass is foundation-first, not full visual polish**
งานรอบแรกเน้น:
- theme tokens
- app shell
- shared layout
- shared reusable UI patterns ที่กระทบหลายหน้า

งานรอบแรก **ไม่ใช่** page-by-page redesign ทุกหน้าของระบบ

---

## 🧩 7. Proposed Theme Token Model

### **A: Minimum semantic tokens for first pass**
ควรเพิ่ม token กลางอย่างน้อยสำหรับ:
- `--color-brand-primary`
- `--color-brand-primary-strong`
- `--color-brand-secondary`
- `--color-brand-secondary-strong`
- `--color-surface`
- `--color-surface-muted`
- `--color-text-primary`
- `--color-text-secondary`
- `--color-border`
- `--color-action-primary`
- `--color-action-primary-hover`
- `--color-action-secondary`
- `--color-focus-ring`

### **B: First-pass token extensions that are allowed**
ถ้าจำเป็นต่อ shared shell รอบแรก อนุญาตให้เพิ่มเฉพาะ:
- `--color-nav-active`
- `--color-header-bg`
- `--color-backoffice-sidebar`
- `--color-backoffice-sidebar-active`

ไม่ควรแตก token ยิบย่อยเกินจำเป็นตั้งแต่วันแรก

### **C: Naming principle**
ห้ามตั้ง token ที่ผูกกับ implementation เกินไป เช่น:
- `--pink-button`
- `--blue-card`

ควรใช้ semantic naming ที่เปลี่ยนสีภายหลังได้โดยไม่ทำให้ชื่อหลอก

### **D: Palette values must be locked before implementation starts**
ก่อนเริ่มลงมือจริง ต้องสรุปค่า hex หรือ token values ที่ใช้จริงให้ชัด ไม่ใช่ปล่อยให้แต่ละไฟล์เลือกสีเอาเอง

### **E: Locked raw school colors and first-pass usable palette**
ค่าสีจาก reference ที่ล็อกแล้ว:
- school pink = `#FF66C4`
- school blue = `#029DE3`

ข้อสรุปรอบแรก:
- `#029DE3` ใช้เป็น `brand primary`
- `#FF66C4` ใช้เป็น `brand secondary / accent`
- primary action ไม่ใช้ชมพู base ตรง ๆ
- interactive states ที่สำคัญต้องใช้เฉดที่เข้มกว่า base color

usable palette รอบแรกที่แนะนำ:

**Brand**
- `--color-brand-primary-soft`: `#E6F5FC`
- `--color-brand-primary`: `#029DE3`
- `--color-brand-primary-strong`: `#016F9F`
- `--color-brand-secondary-soft`: `#FFE5F4`
- `--color-brand-secondary`: `#FF66C4`
- `--color-brand-secondary-strong`: `#D93A9D`

**Neutral**
- `--color-surface`: `#FFFFFF`
- `--color-surface-muted`: `#F8FAFC`
- `--color-text-primary`: `#0F172A`
- `--color-text-secondary`: `#475569`
- `--color-border`: `#E2E8F0`

**Action**
- `--color-action-primary`: `#016F9F`
- `--color-action-primary-hover`: `#01557A`
- `--color-action-secondary`: `#FFE5F4`
- `--color-focus-ring`: `#7DD3FC`

### **F: Locked first-pass usage rules for these values**
- ฟ้าเป็น primary brand language ของระบบ
- ชมพูเป็น accent เท่านั้น ไม่ใช่ CTA หลักของระบบ
- `--color-action-primary` และ `--color-action-primary-hover` ใช้กับ primary button, active nav, และ interactive emphasis หลัก
- `--color-brand-primary-soft` และ `--color-brand-secondary-soft` ใช้กับ tinted background, badge, หรือ selected state ที่ไม่ใช่ CTA
- neutral colors ยังเป็นฐานหลักของ text, surface, border, และ backoffice readability

---

## 🎯 8. Exact First-Pass Scope

### **A: In scope**
รอบแรกให้ทำเฉพาะสิ่งต่อไปนี้:
- สรุป palette direction และ derived tones ที่ใช้ได้จริง
- เพิ่ม semantic tokens ใน `csr-frontend/src/style.css`
- ปรับ `csr-frontend/src/App.vue`
- ปรับ `csr-frontend/src/components/MainLayout.vue`
- ปรับ `csr-frontend/src/components/BackofficeLayout.vue`
- ปรับ `csr-frontend/src/components/FrontofficePageHeader.vue` ถ้าใช้จริงใน shell flow หลัก
- ปรับ shared button/card/shell patterns ที่ถูกใช้ซ้ำหลายหน้าและกระทบภาพรวมชัดเจน

### **B: Explicitly not required in first pass**
รอบแรกยังไม่บังคับให้:
- ไล่เปลี่ยนทุก view ใน `csr-frontend/src/views/**`
- เก็บ one-off decorative sections
- ปรับ gradient/polish รายหน้า
- normalize ทุก component ในระบบ
- รีแฟกเตอร์ upload-related UI ทั้งชุดถ้ายังไม่ใช่ shared foundation จริง

### **C: `PhotoUploadField.vue` is follow-up unless it blocks shell consistency**
ไฟล์นี้มีสี hardcode จริง แต่ยังไม่ควรถูกยัดเป็น scope บังคับของรอบแรก ถ้าไม่ได้ทำให้ shell/theme หลักขัดกันอย่างชัดเจน

---

## 🚫 9. Explicit Non-Goals for First Pass
รอบแรก **ไม่ควร** พยายามทำสิ่งต่อไปนี้พร้อมกัน:

- redesign layout ใหม่ทั้งระบบ
- เปลี่ยน typography ทั้งโปรเจกต์
- ทำ dark mode
- ทำ visual polish รายหน้าจนครบทุกจุด
- rebrand status colors เช่น success/warning/error ทั้งหมดตามสีโรงเรียน
- ใช้ gradient หนักทุก section จนรก
- เปลี่ยนหน้า detail/list/table ทุกจุดเพียงเพื่อไล่ลบสีเก่าให้หมด

อย่าทำงานนี้ให้กลายเป็น “รีดีไซน์ทั้งแอป” เพราะจะบานโดยไม่จำเป็น

---

## 🗂️ 10. Target Files for First Pass

### **Must-change first-pass files**
- `csr-frontend/src/style.css`
- `csr-frontend/src/App.vue`
- `csr-frontend/src/components/MainLayout.vue`
- `csr-frontend/src/components/BackofficeLayout.vue`

### **Conditional first-pass files**
- `csr-frontend/src/components/FrontofficePageHeader.vue`
- shared reusable UI files ที่ทำหน้าที่เป็น button/card/section shell และถูกใช้ซ้ำหลายหน้า

### **Deferred by default**
- `csr-frontend/src/views/**`
- component เฉพาะจุดที่ไม่ได้เป็น shared foundation

---

## ⚙️ 11. Recommended Delivery Order

### **Step 1: Lock palette values and usage roles**
- ยืนยันค่าสีจริงของโรงเรียนจากไฟล์อ้างอิงหรือสรุปเป็น hex ที่ชัดเจน
- ล็อกให้ชัดว่า blue = primary, pink = accent
- สรุป derived tones สำหรับ interactive usage

### **Step 2: Build semantic tokens in one place**
- เพิ่ม tokens กลางใน `src/style.css`
- แยก brand, neutral, action, text, border, focus
- ล็อกชื่อ token ให้ใช้ร่วมกันได้จริง

### **Step 3: Refactor shared shells first**
- เปลี่ยน `App.vue`
- เปลี่ยน `MainLayout.vue`
- เปลี่ยน `BackofficeLayout.vue`

### **Step 4: Refactor only the shared UI patterns that matter**
- primary button pattern
- common shell/card/section wrapper
- shared header pattern ที่ใช้จริงในหลายหน้า

### **Step 5: Validate before expanding scope**
- ตรวจ front-office shell
- ตรวจ backoffice shell
- ตรวจ contrast, focus, active states
- ถ้ายังไม่นิ่ง ห้ามขยายไป page sweep

---

## ✅ 12. Implementation Checklist

### **Palette / direction**
- [ ] สรุปค่าสีโรงเรียนที่ใช้งานจริงได้
- [ ] ล็อกว่า blue = primary และ pink = accent
- [ ] สร้างเฉด derived สำหรับ interactive states

### **Theme foundation**
- [ ] เพิ่ม semantic theme tokens ใน `csr-frontend/src/style.css`
- [ ] เลิกพึ่ง `--color-primary` ตัวเดียวเป็นฐานหลัก
- [ ] แยก token สำหรับ text/surface/border/action/focus

### **Shared shell migration**
- [ ] ปรับ `App.vue` ให้ใช้สีพื้นฐานที่สอดคล้องกับ theme ใหม่
- [ ] ปรับ `MainLayout.vue`
- [ ] ปรับ `BackofficeLayout.vue`
- [ ] ลบการพึ่ง brand hardcoded เดิมใน shared shell หลักเท่าที่เกี่ยวข้องกับ theme system

### **Shared UI patterns**
- [ ] ปรับ shared button/card/section shell ที่กระทบหลายหน้าจริง
- [ ] ลดการอ้าง `emerald/slate/teal` ตรง ๆ ใน component กลางที่ถูกแตะในรอบนี้
- [ ] ย้ายสีที่ใช้ซ้ำให้ไปอิง token กลาง

### **Verification**
- [ ] ตรวจหน้า mobile front-office shell
- [ ] ตรวจหน้า mobile backoffice shell
- [ ] ตรวจ text/background contrast
- [ ] ตรวจ active/hover/focus states
- [ ] ตรวจว่าปุ่มหลักยังเด่นและกดใช้งานง่าย

### **Documentation**
- [ ] อัปเดต `README.md` ถ้ามีการเพิ่มแนวทาง theme token หรือ styling convention ที่ควรรู้

---

## 🚦 13. Completion Gate for First Pass
จะถือว่ารอบแรก “เสร็จ” ก็ต่อเมื่อครบทั้งหมดนี้:

- `style.css` มี semantic tokens ที่ใช้จริง ไม่ใช่เพิ่มไว้เฉย ๆ
- `App.vue`, `MainLayout.vue`, และ `BackofficeLayout.vue` migrate ไปใช้ theme direction ใหม่แล้ว
- shared shell หลักไม่พึ่งพา hardcoded `emerald/slate/teal` สำหรับ brand/theme หลักอีกต่อไป
- front-office shell และ backoffice shell ใช้ language เดียวกัน แต่ backoffice ยังอ่านง่ายและนิ่งกว่า
- ปุ่มหลัก, nav active state, และ focus state ชัดพอใช้งานจริง
- ยังไม่มี evidence ว่า contrast พังในหน้าหลักที่ตรวจ

ถ้าข้อใดข้อหนึ่งยังไม่ผ่าน ให้ถือว่างานรอบแรกยังไม่จบ

---

## 🧪 14. Verification Approach

### **A: Manual screenshot baseline is required**
อย่างน้อยควรเทียบ before/after ในหน้าต่อไปนี้:
- front-office shell หรือ dashboard
- หน้าที่มี page header / navigation ชัด
- backoffice dashboard หรือ student list
- mobile navigation state

### **B: Operational contrast rules for this task**
รอบแรกให้ยึดกติกาง่าย ๆ นี้:
- ห้ามใช้ brand soft color เป็น text หลักบนพื้นขาว
- ห้ามใช้ brand light tone เป็น primary button ถ้า text อ่านยาก
- focus ring ต้องเห็นชัดบนพื้นขาวและ tinted background
- backoffice table/list/detail text ต้องยังคงอ่านง่ายด้วย neutral text เป็นหลัก

### **C: Do not expand scope during verification**
ถ้าระหว่างตรวจพบหน้าอื่นยังมีสีเก่า:
- ให้จดเป็น follow-up
- อย่าขยายงานรอบแรกทันที เว้นแต่ไฟล์นั้นทำให้ shared shell ไม่สอดคล้องอย่างชัดเจน

---

## 🧪 15. Acceptance Criteria
- เว็บมีสีและ mood ใกล้กับ theme โรงเรียนมากขึ้นอย่างชัดเจน
- UI หลักยังอ่านง่ายและใช้งานได้จริง ไม่ซีดจนเสีย usability
- front-office และ backoffice มีทิศทางสีที่สอดคล้องกัน แต่ไม่จำเป็นต้องใช้ความเข้มเท่ากัน
- `style.css`, `App.vue`, `MainLayout.vue`, และ `BackofficeLayout.vue` ใช้ theme direction ใหม่อย่างชัดเจน
- shared shell หลักอิง semantic tokens มากขึ้น ไม่ผูกกับสี hardcode เดิมแบบกระจัดกระจาย
- ปุ่มหลัก, nav active state, และ focus states ยังชัดเจนพอ
- status colors ที่เป็น success/warning/error ไม่ถูกทำให้สับสนกับ brand colors
- hardcoded theme colors หลักใน shared shell ถูกลดลงอย่างมีนัยสำคัญ

---

## ⚠️ 16. Risks / Notes
- ถ้าไม่ล็อก palette values ก่อน implementation จะเกิดการตีความสีคนละแบบทันที
- ถ้ารีบเปลี่ยนสีแบบหน้าใครหน้ามัน จะได้ UI ที่คนละภาษาและเก็บงานยาก
- ถ้าใช้สีอ่อนของโรงเรียนตรง ๆ เป็นปุ่มหลัก ระบบจะดูไม่พร้อมใช้งานและ contrast จะตก
- ถ้าเอา brand color ไปแทน status colors ด้วย จะทำให้ semantics ของ UI พัง
- ถ้าพยายาม redesign ทั้งระบบพร้อมกับ refactor theme งานจะบานเกิน scope ทันที
- ถ้า verification ไม่มี screenshot baseline หรือจุดเช็กที่ชัด การถกเรื่อง “สวยขึ้นหรือยัง” จะไม่จบ

---

## 🧠 17. Honest Recommendation
ข้อแนะนำแบบตรงไปตรงมา:
- อย่าพยายาม “ซื่อสัตย์กับไฟล์สี” จนทำให้เว็บใช้งานแย่ลง
- ให้ซื่อสัตย์กับ **brand direction** แต่ปรับเฉดเพื่อให้ใช้งานจริงได้
- เริ่มจาก **theme foundation** ก่อนเสมอ
- ใช้ **ฟ้าเป็น primary**, **ชมพูเป็น accent**, และให้ neutral คุม readability
- ถ้ายังไม่ได้ lock palette values จริง อย่าเพิ่งเริ่ม implementation
- ถ้ารอบแรกยังไม่ผ่าน completion gate ห้ามบานไป page-by-page polish

ถ้าทำตามนี้ งานจะออกมาเป็นระบบ และเปลี่ยน theme ได้จริงโดยไม่ทิ้งหนี้เทคนิคเพิ่ม
