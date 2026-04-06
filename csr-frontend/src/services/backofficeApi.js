import { apiJson, apiBlob } from './apiClient';

export async function fetchCurrentUser(token) {
  return apiJson('/api/backoffice/me', token);
}

export async function fetchDashboard(token) {
  return apiJson('/api/backoffice/dashboard', token);
}

export async function fetchStudents(token) {
  return apiJson('/api/backoffice/students', token);
}

export async function fetchStudentDetail(id, token) {
  return apiJson(`/api/backoffice/students/${id}`, token);
}

export async function updateStudentStatus(id, status, token) {
  return apiJson(`/api/backoffice/students/${id}/status`, token, {
    method: 'PUT',
    body: JSON.stringify({ status }),
    headers: { 'Content-Type': 'application/json' }
  });
}

export async function updateStudentNote(id, note, token) {
  return apiJson(`/api/backoffice/students/${id}/note`, token, {
    method: 'PUT',
    body: JSON.stringify({ internalNote: note }),
    headers: { 'Content-Type': 'application/json' }
  });
}

export async function fetchStudentPhotoBlob(id, token) {
  return apiBlob(`/api/backoffice/students/${id}/student-photo`, token);
}

export async function fetchGuardianPhotoBlob(id, order, token) {
  return apiBlob(`/api/backoffice/students/${id}/guardians/${order}/photo`, token);
}

// Staff management
export async function fetchStaffList(token) {
  return apiJson('/api/backoffice/staff', token);
}

export async function createStaff({ lineUserId, name, role }, token) {
  return apiJson('/api/backoffice/staff', token, {
    method: 'POST',
    body: JSON.stringify({ lineUserId, name, role }),
    headers: { 'Content-Type': 'application/json' }
  });
}

export async function updateStaff(id, { role, name }, token) {
  return apiJson(`/api/backoffice/staff/${id}`, token, {
    method: 'PUT',
    body: JSON.stringify({ role, name }),
    headers: { 'Content-Type': 'application/json' }
  });
}

export async function deleteStaff(id, token) {
  return apiJson(`/api/backoffice/staff/${id}`, token, {
    method: 'DELETE'
  });
}
