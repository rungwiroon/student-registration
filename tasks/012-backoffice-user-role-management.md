# Task 012 — Backoffice User & Role Management

## Goal
Provide a backoffice UI for Teachers to manage which LINE User IDs are mapped to which role (`Teacher` or `ParentNetworkStaff`).

## Current State
- `StaffUser` table maps `LineUserId` → `Role` (`Teacher` | `ParentNetworkStaff`)
- Role assignment done via dev-only script or direct DB edits
- Backend authorization (`IBackofficePolicy` + endpoint filters) already works
- Frontend reads capabilities from `GET /api/backoffice/me`

## Scope

### Backend
- [x] `GET /api/backoffice/staff` — list all staff users (Teacher-only access)
- [x] `POST /api/backoffice/staff` — add a new LINE User ID with a role (Teacher-only)
- [x] `PUT /api/backoffice/staff/{id}` — change a user's role (Teacher-only)
- [x] `DELETE /api/backoffice/staff/{id}` — remove a user's access (Teacher-only)

### Frontend
- [x] Staff management page at `/backoffice/staff` (accessible only to Teachers)
- [x] Show list of current staff: name, LINE User ID, role
- [x] Add new staff: input LINE User ID, name, select role
- [x] Change role: toggle between Teacher / ParentNetworkStaff
- [x] Remove staff: confirm dialog, then delete

### Data Model
- [x] Add `CreatedAt` to `StaffUser` table
- [x] Add `IsActive` flag (soft delete instead of hard delete)

## Out of Scope
- LINE Login or LIFF configuration changes
- Self-registration / approval workflows
- Audit log
- Granular per-feature permissions (keep role-based)
- Multiple roles per user
