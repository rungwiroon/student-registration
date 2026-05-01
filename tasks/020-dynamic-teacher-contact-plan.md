# Plan: Dynamic Teacher Contact in Contacts Page

**Status:** Pending approval  
**Depends on:** `useLiff.js`, `Contacts.vue`, backoffice staff management  
**Goal:** Replace hardcoded `ครูที่ปรึกษา` in Contacts with live API data + per-teacher visibility toggle.

---

## Context

`Contacts.vue` currently hardcodes a teacher card (`อ.สมรักษ์ รักเรียน`, phone `0891112222`). The `เครือข่ายผู้ปกครอง` section below already fetches from `/api/directory`. We need to pull teacher data from API too, and add per-teacher show/hide control.

---

## Chosen Approach

Extend existing `StaffUser` entity with `Phone`, `Position`, and `IsVisibleInDirectory`. Reuse staff-management UI/endpoints — no new repository or table.

**Why not new entity?** `StaffUser` already models staff (Teacher / ParentNetworkStaff). Separate `Advisor`/`Committee` table duplicates CRUD plumbing. Unused `Committee` model also has encrypted fields — overkill for public phone.

---

## Backend Changes

### 1. Extend `StaffUser` model
**File:** `CsrApi/Models/StaffUser.cs`
- Add `public string Phone { get; set; } = string.Empty;`
- Add `public string Position { get; set; } = string.Empty;`
- Add `public bool IsVisibleInDirectory { get; set; } = true;`

### 2. Extend `StaffRepository`
**File:** `CsrApi/Repositories/StaffRepository.cs`
- In `InitializeDatabaseAsync`: add `ALTER TABLE` migrations for `Phone`, `Position`, `IsVisibleInDirectory` (same pattern as existing `IsActive`/`CreatedAt` migrations).
- In `UpsertStaffUserAsync`: include new columns in `INSERT ... ON CONFLICT`.

### 3. Extend backoffice staff endpoints
**File:** `CsrApi/BackofficeEndpoints.cs`
- `CreateStaffRequest`: add optional `Phone`, `Position`, `IsVisibleInDirectory`.
- `UpdateStaffRequest`: add optional `Phone`, `Position`, `IsVisibleInDirectory`.
- `POST /api/backoffice/staff`, `PUT /api/backoffice/staff/{id}`: handle new fields.
- `GET /api/backoffice/staff`: project new fields in response.

### 4. Extend `/api/directory` endpoint
**File:** `CsrApi/Program.cs`
- Filter: `Role == "Teacher" && IsActive && IsVisibleInDirectory`.
- Return shape:
  ```json
  {
    "teachers": [
      { "id": "...", "name": "...", "phone": "...", "position": "..." }
    ],
    "parentNetwork": [
      { "id": "...", "name": "..." }
    ]
  }
  ```

---

## Frontend Changes

### 1. `Contacts.vue`
**File:** `csr-frontend/src/views/Contacts.vue`
- Add `teachers` ref.
- Update `/api/directory` handler to destructure `teachers` + `parentNetwork`.
- Replace hardcoded `<section>` with `v-if="teachers.length > 0"` loop.
- Render `person.position` as subtitle, `tel:` link from `person.phone`.
- Empty state: show `ยังไม่มีข้อมูล`.
- Hide entire section when `teachers.length === 0`.

### 2. `StaffManagementView.vue`
**File:** `csr-frontend/src/views/backoffice/StaffManagementView.vue`
- Add `Phone`, `Position`, and `แสดงในรายชื่อติดต่อ` inputs in add-staff form.
- Display new fields in staff table (desktop + mobile).
- Allow inline edit / toggle of `IsVisibleInDirectory`.

### 3. `backofficeApi.js`
**File:** `csr-frontend/src/services/backofficeApi.js`
- `createStaff`: expand destructuring from `{ lineUserId, name, role }` to `{ lineUserId, name, role, phone, position, isVisibleInDirectory }` and include them in the `POST` body.
- `updateStaff`: expand destructuring from `{ role, name }` to `{ role, name, phone, position, isVisibleInDirectory }` and include them in the `PUT` body.

---

## Verification

1. Start backend + frontend.
2. Backoffice → Staff Management → add/edit Teacher with phone, position, visibility.
3. Contacts page → teacher card renders with API data, phone link works.
4. Backoffice → set `IsVisibleInDirectory = false` → reload Contacts → teacher disappears.
5. Disable all visible teachers → section shows `ยังไม่มีข้อมูล`.

---

## Out of Scope

- Global config flag to hide entire section (per-teacher toggle sufficient).
- Encrypting phone numbers (public contact info).
- Changing unused `Committee` model.
