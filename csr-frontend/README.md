# CSR Frontend

## Development

```bash
npm install
npm run dev
```

## Build

```bash
npm run build
```

## Registration architecture

- `src/views/Register.vue` เป็นตัว orchestration ของหน้า register/edit profile
- `src/composables/useRegistrationForm.js` ดูแล form state, profile loading, และ submit flow
- `src/components/StudentPhotoUpload.vue` และ `src/components/GuardianPhotoUpload.vue` แยก UI upload รูปออกจากหน้า register
- `src/services/registrationApi.js` ส่งข้อมูลแบบ `multipart/form-data` เพื่ออัปโหลดข้อมูลลงทะเบียนพร้อมรูปผ่าน backend
- รูปเดิมถูกดึงผ่าน protected API แล้วแปลงเป็น blob URL ใน browser จึงไม่ต้องเปิด public static serving
