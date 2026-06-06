# วิธีสร้าง Google Service Account สำหรับ Google Sheets API

## Step 1: เข้า Google Cloud Console

1. ไปที่ https://console.cloud.google.com/
2. Login ด้วย Google Account (ควรใช้บัญชีโรงเรียน/องค์กร)
3. สร้าง Project ใหม่ หรือเลือก Project ที่มีอยู่

## Step 2: เปิด Google Sheets API

1. ใน Console ไปที่ **APIs & Services** → **Library**
2. ค้นหา "Google Sheets API"
3. กด **Enable** (เปิดใช้งาน)

## Step 3: สร้าง Service Account

1. ไปที่ **APIs & Services** → **Credentials**
2. กด **+ Create Credentials** → เลือก **Service Account**
3. ตั้งชื่อ:
   - Service account name: `csr-shirt-order` (หรือชื่ออะไรก็ได้)
   - Service account ID: `csr-shirt-order` (auto-generate)
   - Description: `For CSR School Shirt Order appending to Google Sheets`
4. กด **Create and Continue**
5. Grant access (optional): เลือก **Basic** → **Editor** (หรือไม่ต้องเลือกก็ได้)
6. กด **Done**

## Step 4: สร้าง Key (JSON)

1. ใน Credentials page หา Service Account ที่เพิ่งสร้าง
2. คลิกที่ชื่อ Service Account → ไปที่ tab **Keys**
3. กด **Add Key** → **Create new key**
4. เลือก **JSON**
5. กด **Create**

📥 ไฟล์ `.json` จะดาวน์โหลดอัตโนมัติ (เช่น `project-id-xxx-service-account.json`)

**⚠️ ระวัง**: ไฟล์นี้มี secret key อย่า commit ขึ้น Git!

## Step 5: แชร์ Google Sheet

1. สร้าง Google Sheet ใหม่ (หรือใช้ sheet ที่มีอยู่)
2. คัดลอก **Email** ของ Service Account (ดูที่ Service Account details)
   - รูปแบบ: `csr-shirt-order@project-id.iam.gserviceaccount.com`
3. ใน Google Sheet กด **Share** → ใส่ email นั้น → เลือกสิทธิ์ **Editor**
4. กด **Share**

## Step 6: หา Spreadsheet ID

Spreadsheet ID คือส่วนใน URL ระหว่าง `/d/` และ `/edit`

```
https://docs.google.com/spreadsheets/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/edit
#                                                   ^^^^^^^^^^^^^^^^^^^^^^^^^^^^
#                                                   นี่คือ Spreadsheet ID
```

## Step 7: วางไฟล์ในเครื่อง/เซิร์ฟเวอร์

### วิธี A: Local Dev (ไม่ต้อง Docker)

```bash
# สมมติดาวน์โหลดมาไว้ที่ Downloads
mv ~/Downloads/project-id-xxx-service-account.json \
   /Users/chnbmac02/Documents/personal/sourcecode/student-registration/CsrApi/App_Data/GoogleCredentials/service-account.json

# แก้ appsettings.json (หรือ .env)
# "GoogleSheets": {
#   "CredentialsPath": "App_Data/GoogleCredentials/service-account.json",
#   "SpreadsheetId": "1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms",
#   "SheetName": "Orders"
# }
```

### วิธี B: Production Docker (VPS)

1. Copy ไฟล์ขึ้น VPS (ผ่าน scp)

```bash
scp ~/Downloads/project-id-xxx-service-account.json \
    user@your-vps:/var/csr/google-service-account.json
```

2. แก้ `docker-compose.yml` mount volume:

```yaml
volumes:
  - app_data:/app/data
  - /var/csr:/app/credentials:ro
```

3. หรือใส่ใน `.env`:

```bash
GOOGLE_CLOUD_CREDENTIALS_PATH=/app/credentials/google-service-account.json
GOOGLE_SHEETS_SPREADSHEET_ID=1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms
```

## Step 8: ทดสอบ

```bash
# Restart backend
# แล้วลอง submit order ผ่านหน้าเว็บ
# เช็ค Google Sheet ว่ามี row ใหม่ขึ้นไหม
```

---

## 🔒 Security Checklist

- [ ] `google-service-account.json` อยู่ใน `.gitignore`
- [ ] ไฟล์ JSON ไม่ถูก commit ขึ้น repository
- [ ] ไฟล์ JSON ถูก mount เป็น `read-only` (`:ro`) ใน Docker
- [ ] Service Account มีสิทธิ์แค่ **Editor** ใน Sheet นั้น (ไม่ใช่ Owner ทั้ง project)
- [ ] Sheet ไม่ได้แชร์แบบ public (แชร์เฉพาะ Service Account email)
