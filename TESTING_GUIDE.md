# 🧪 Pre-Deployment Testing Guide — School Shirt Order

## ต้องเตรียมอะไรก่อน

1. `.env` ไฟล์ที่มีค่าครบ (copy จาก `.env.example` แล้วแก้)
2. รูปเสื้อจริงใน `csr-frontend/public/images/` (หรือใช้ placeholder ก่อนก็ได้)
3. Google Service Account JSON (ถ้าจะเทส Google Sheets — ไม่เทสก็ได้)

---

## 🎯 วิธีที่ 1: ทดสอบแบบเร็ว (Local Dev — ไม่ต้อง Docker)

### Step 1: Start Backend

```bash
cd CsrApi
dotnet run
```

รอจนกว่าจะขึ้น `Now listening on: http://localhost:8080`

### Step 2: Start Frontend (อีก Terminal หนึ่ง)

```bash
cd csr-frontend
npm run dev
```

รอจนกว่าจะขึ้น `Local: http://localhost:5173/`

### Step 3: เปิด Browser เทส Manual

1. ไปที่ http://localhost:5173/dashboard
2. กดปุ่ม 🎽 **สั่งซื้อเสื้อ POLO**
3. ตรวจสอบ:
   - [ ] รูปแบบ A และ B แสดง
   - [ ] ตารางไซส์แสดงครบ XS–7XL
   - [ ] กด `+` เพิ่มจำนวน → total เปลี่ยน
   - [ ] กด `-` ลดจำนวน → total เปลี่ยน
   - [ ] กรอกชื่อ + เลขที่
   - [ ] แนบสลิป (เลือกไฟล์ jpg/png)
   - [ ] กด "ยืนยัน" → ขึ้น success state

### Step 4: เช็คไฟล์ที่บันทึก

```bash
# ดูว่าสลิปถูกบันทึกไหม
ls -la CsrApi/App_Data/ShirtOrderSlips/

# ดูว่า order ถูกบันทึกไหม (ถ้า config Google Sheets)
# ไปดูใน Google Sheet ที่คุณ config ไว้
```

---

## 🎯 วิธีที่ 2: API Testing ด้วย curl

เปิด Terminal ใหม่แล้วรัน:

```bash
# 1. Test health check
curl http://localhost:8080/health
# Expected: Healthy

# 2. Test without auth → 401
curl -X POST http://localhost:8080/api/v1/school-shirt/order \
  -F "payload={\"studentName\":\"Test\",\"studentNumber\":\"1\",\"items\":[],\"totalAmount\":0}"
# Expected: 401 Unauthorized

# 3. Test with mock auth but no slip → 400
curl -X POST http://localhost:8080/api/v1/school-shirt/order \
  -H "Authorization: Bearer mock-token" \
  -F "payload={\"studentName\":\"Test\",\"studentNumber\":\"1\",\"items\":[{\"design\":\"A\",\"size\":\"M\",\"quantity\":1,\"unitPrice\":250}],\"totalAmount\":250}"
# Expected: 400 "Proof of payment slip is required."

# 4. Test with mock auth + slip → 200 (สร้าง dummy slip ก่อน)
echo "fake-slip-data" > /tmp/test-slip.png
curl -X POST http://localhost:8080/api/v1/school-shirt/order \
  -H "Authorization: Bearer mock-token" \
  -F "payload={\"studentName\":\"Test\",\"studentNumber\":\"1\",\"items\":[{\"design\":\"A\",\"size\":\"M\",\"quantity\":1,\"unitPrice\":250}],\"totalAmount\":250}" \
  -F "proofOfPayment=@/tmp/test-slip.png"
# Expected: {"message":"Order received","orderId":"...","slipUrl":"..."}
```

---

## 🎯 วิธีที่ 3: Docker Compose Testing (ใกล้เคียง Production มากที่สุด)

อันนี้สำคัญที่สุด — เพราะ production ใช้ Docker

### Step 1: สร้าง `.env` ให้ครบ

```bash
cp .env.example .env
```

แล้วแก้ `.env` ให้มีค่าอย่างน้อย:

```bash
CONNECTION_STRING=Data Source=/app/data/csrapi.db;Password=test1234;
ENCRYPTION_KEY=12345678901234567890123456789012
LINE_LIFF_CHANNEL_ID=your-channel-id
VITE_LIFF_ID=your-liff-id
VITE_USE_MOCK_LIFF=true
LINE_USE_MOCK_AUTH=true
LINE_MOCK_USER_ID=mock-user-123
BOOTSTRAP_SECRET_TOKEN=test-secret-123
GOOGLE_SHEETS_SPREADSHEET_ID=your-sheet-id-or-leave-empty
SHIRT_ORDER_SLIP_PATH=App_Data/ShirtOrderSlips
```

### Step 2: Build Docker Images

```bash
# Build ทั้ง backend + frontend
docker compose build --no-cache
```

หรือใช้ script deploy ที่มีอยู่แล้ว:

```bash
./scripts/deploy.sh
```

### Step 3: Start Containers

```bash
docker compose up -d
```

### Step 4: Verify

```bash
# เช็คว่า container ทำงาน
docker compose ps

# เช็ค log ถ้ามีปัญหา
docker compose logs api --tail=50
docker compose logs frontend --tail=20

# เช็ค health
curl http://localhost:8080/health
```

### Step 5: Browser Test

ไปที่ http://localhost:5173 (frontend proxy)
หรือ http://localhost:8080 (API ตรง)

### Step 6: Cleanup

```bash
docker compose down
docker compose down -v  # ลบ volume ด้วยถ้าจะเริ่มใหม่หมด
```

---

## 🎯 วิธีที่ 4: Playwright E2E Tests

ต้อง start backend + frontend dev server ก่อน (แบบวิธีที่ 1)

```bash
# Terminal 1
cd CsrApi && dotnet run

# Terminal 2
cd csr-frontend && npm run dev

# Terminal 3 (รอให้ 2 อันข้างบน start เสร็จก่อน ~10 วินาที)
cd csr-frontend
npx playwright test tests/e2e/frontoffice/shirt-order.spec.js --reporter=list
```

ถ้าอยากเห็นหน้าจอขณะ test:

```bash
npx playwright test tests/e2e/frontoffice/shirt-order.spec.js --headed
```

ถ้าอยาก debug UI mode:

```bash
npx playwright test --ui
```

---

## 🔍 Troubleshooting

| ปัญหา | สาเหตุ | แก้ไข |
|--------|--------|--------|
| `ERR_CONNECTION_REFUSED` | Backend หรือ Frontend ยังไม่ start | รอให้ทั้ง 2 service start ก่อน |
| `401 Unauthorized` | LIFF token ไม่ถูกต้อง | เช็ค `VITE_USE_MOCK_LIFF=true` ใน `.env` |
| `400 Bad Request` | ส่งข้อมูลไม่ครบ | เช็คว่าส่ง `payload` + `proofOfPayment` |
| `500 Internal Server Error` | Google Sheets config ผิด | เช็ค `GOOGLE_SHEETS_SPREADSHEET_ID` หรือ credentials path |
| รูปเสื้อไม่แสดง | ไม่มีไฟล์รูป | ใส่รูปจริงใน `public/images/` |
| Docker build ช้า | Build cache | ลอง `docker compose build --no-cache` |
| Slip ไม่ถูกบันทึก | Volume permission | เช็คว่า `App_Data` เป็น writable |

---

## ✅ Final Checklist Before Deploy

- [ ] Backend compiles (`dotnet build` ผ่าน 0 error)
- [ ] Frontend builds (`npm run build` ผ่าน)
- [ ] Docker compose up ได้ (`docker compose up -d`)
- [ ] API health check ตอบกลับ (`curl /health` → Healthy)
- [ ] ส่ง order ผ่าน curl ได้ (`curl /api/v1/school-shirt/order` → 200)
- [ ] หน้าเว็บ load ได้ (`http://localhost:5173/shirt-order`)
- [ ] รูปเสื้อแสดง (ไม่ใช่ placeholder)
- [ ] ข้อมูลบัญชีธนาคารถูกต้อง
- [ ] `.env` มีค่าครบ (ไม่มี `REPLACE_WITH_...`)
- [ ] Google Service Account JSON อยู่ในที่ที่ mount ไว้ (ถ้าใช้ Google Sheets)
- [ ] Playwright tests ผ่านอย่างน้อย validation + navigation tests
