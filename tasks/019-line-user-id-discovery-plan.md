# Plan: Discover LINE User IDs for Admin Assignment

## Context
- `LineUserId` already stored on `Guardians` table for primary guardian (`GuardianOrder == 1`) during registration (`RegistrationService.cs:400`).
- Backoffice `StaffManagementView.vue` requires manual entry of `LineUserId` when creating staff users.
- No easy way for admin to discover registered `LineUserId`s.

## Goal
Make registered LINE user IDs discoverable in backoffice so admin can copy-paste them when assigning staff roles.

## Changes

### Backend
1. **Repository** (`CsrApi/Repositories/StudentRepository.cs`)
   - Add `GetGuardiansWithLineUserIdAsync()` to `IStudentRepository` and `StudentRepository`
   - Query: `SELECT * FROM Guardians WHERE LineUserId IS NOT NULL AND LineUserId != '' ORDER BY GuardianOrder ASC`

2. **Endpoint** (`CsrApi/BackofficeEndpoints.cs`)
   - Add `GET /api/backoffice/registered-users` (Teacher-only)
   - Returns list with: `lineUserId`, `guardianName`, `guardianPhone`, `relationType`, `guardianOrder`, `studentName`, `studentId`
   - Decrypt names/phones using `IEncryptionService`
   - Look up student name per guardian
   - Also expose `LineUserId` in existing `GET /api/backoffice/students/{id}` guardian response

### Frontend
3. **API** (`csr-frontend/src/services/backofficeApi.js`)
   - Add `fetchRegisteredUsers(token)`

4. **View** (`csr-frontend/src/views/backoffice/RegisteredUsersView.vue`)
   - Table/card list of registered LINE users
   - Show `lineUserId` (copy button), guardian name, student name, relation
   - Link/button to pre-fill staff creation form (or just copy ID)

5. **Route** (`csr-frontend/src/router/index.js`)
   - Add `/backoffice/registered-users` route

6. **Layout** (`csr-frontend/src/components/BackofficeLayout.vue`)
   - Add nav link "ผู้ใช้ที่ลงทะเบียน" (Teacher-only)

7. **StudentDetailView** (`csr-frontend/src/views/backoffice/StudentDetailView.vue`)
   - Show `lineUserId` on guardian card for quick copy

## Verify
- [x] `GET /api/backoffice/registered-users` returns 200 with array of guardians that have LineUserId
- [x] `GET /api/backoffice/students/{id}` includes `lineUserId` in guardian objects
- [x] Backoffice nav shows new link
- [x] RegisteredUsersView loads and displays data
- [x] StudentDetailView shows LineUserId on guardian cards
