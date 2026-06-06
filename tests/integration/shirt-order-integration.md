# Shirt Order Integration Test Guide

## Pre-requisites
1. Backend running (`dotnet run` in `CsrApi/` or Docker)
2. Frontend built (`npm run build` in `csr-frontend/`)
3. Google Service Account JSON available at configured path (optional — API works without it, just warns)

## Quick API Test (curl)

### Test 1: Unauthenticated request → 401
```bash
curl -X POST http://localhost:8080/api/v1/school-shirt/order \
  -F "payload={\"studentName\":\"Test\",\"studentNumber\":\"1\",\"items\":[],\"totalAmount\":0}"
```
Expected: `401 Unauthorized`

### Test 2: Missing slip → 400
```bash
curl -X POST http://localhost:8080/api/v1/school-shirt/order \
  -H "Authorization: Bearer mock-token" \
  -F "payload={\"studentName\":\"Test\",\"studentNumber\":\"1\",\"items\":[{\"design\":\"A\",\"size\":\"M\",\"quantity\":1,\"unitPrice\":250}],\"totalAmount\":250}"
```
Expected: `400 Bad Request` — "Proof of payment slip is required"

### Test 3: Valid request → 200 (with mock auth)
```bash
curl -X POST http://localhost:8080/api/v1/school-shirt/order \
  -H "Authorization: Bearer mock-token" \
  -F "payload={\"studentName\":\"Test\",\"studentNumber\":\"1\",\"items\":[{\"design\":\"A\",\"size\":\"M\",\"quantity\":1,\"unitPrice\":250}],\"totalAmount\":250}" \
  -F "proofOfPayment=@/path/to/slip.png"
```
Expected: `200 OK` with JSON `{ "message": "Order received", "orderId": "...", "slipUrl": "..." }`

## API Contract

### Request
```
POST /api/v1/school-shirt/order
Content-Type: multipart/form-data
Authorization: Bearer <liff-token>

Fields:
  - payload: JSON string (ShirtOrderRequest)
  - proofOfPayment: File (image/jpeg or image/png, max 5MB)
```

### Request Payload Schema
```json
{
  "lineDisplayName": "คุณแม่สมชาย",
  "studentName": "สมชาย ใจดี",
  "studentNumber": "12",
  "items": [
    { "design": "A", "size": "M", "quantity": 1, "unitPrice": 250 }
  ],
  "totalAmount": 250
}
```

### Response (200 OK)
```json
{
  "message": "Order received",
  "orderId": "abc123...",
  "slipUrl": "/api/v1/school-shirt/slips/{ownerHash}/{filename}"
}
```

### Response (400 Bad Request)
```
"Proof of payment slip is required."
```

### Response (401 Unauthorized)
```
(empty body or standard unauthorized)
```

## Frontend → Backend Integration Checklist

- [x] Frontend sends `payload` field as JSON string
- [x] Frontend sends `proofOfPayment` field as file
- [x] Frontend sends `Authorization: Bearer <token>` header
- [x] Backend extracts `LineUserId` from `HttpContext.Items["LineUserId"]`
- [x] Backend validates file type (jpg/png only)
- [x] Backend saves file to protected folder (hash-based)
- [x] Backend appends row to Google Sheet (if configured)
- [x] Backend returns 200 with `orderId` and `slipUrl`
- [x] Frontend shows success state with order summary

## Google Sheet Integration (Optional)

If `GOOGLE_SHEETS_SPREADSHEET_ID` is configured:
1. Create Google Sheet with sheet name "Orders"
2. Add header row: `Date | LINE Name | Student Name | Student No | Order Summary | Total | Slip URL | Status`
3. Share with service account email
4. Mount credentials JSON via Docker volume

If not configured:
- Backend logs warning but still returns 200 to user
- Order is accepted but not recorded in Google Sheet (use fallback: check slip files in `App_Data/ShirtOrderSlips/`)
