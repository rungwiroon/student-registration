import { apiBlob, apiFetch, apiJson } from './apiClient';

function normalizeOptionalText(value) {
  return value ? value : '';
}

function toPayload(form) {
  return {
    student: {
      studentId: form.student.studentId || null,
      firstName: normalizeOptionalText(form.student.firstName),
      lastName: normalizeOptionalText(form.student.lastName),
      nickname: normalizeOptionalText(form.student.nickname),
      oldRoom: form.student.oldRoom || null,
      oldNo: form.student.oldNo || null,
      newRoom: form.student.newRoom || null,
      newNo: form.student.newNo || null,
      phone: normalizeOptionalText(form.student.phone),
      bloodType: normalizeOptionalText(form.student.bloodType),
      dob: normalizeOptionalText(form.student.dob)
    },
    guardians: form.guardians.map(g => ({
      order: g.order,
      relationType: g.relationType || null,
      firstName: normalizeOptionalText(g.firstName),
      lastName: normalizeOptionalText(g.lastName),
      phone: normalizeOptionalText(g.phone),
      occupation: normalizeOptionalText(g.occupation),
      email: normalizeOptionalText(g.email)
    }))
  };
}

export async function fetchMyProfile(token) {
  return apiJson('/api/me', token);
}

export async function submitRegistration(token, form, files) {
  const formData = new FormData();
  formData.append('payload', JSON.stringify(toPayload(form)));

  if (files.studentPhoto) {
    formData.append('studentPhoto', files.studentPhoto);
  }

  if (files.guardianPhoto1) {
    formData.append('guardianPhoto1', files.guardianPhoto1);
  }

  if (files.guardianPhoto2) {
    formData.append('guardianPhoto2', files.guardianPhoto2);
  }

  const response = await apiFetch('/api/register', token, {
    method: 'POST',
    body: formData
  });

  if (!response.ok) {
    const message = (await response.text()) || 'Registration failed.';
    const error = new Error(message);
    error.status = response.status;
    throw error;
  }

  return response.json();
}

export async function fetchProtectedPhotoUrl(token, path) {
  if (!path) {
    return null;
  }

  try {
    const blob = await apiBlob(path, token);
    return URL.createObjectURL(blob);
  } catch (error) {
    if (error.status === 404) {
      return null;
    }

    throw error;
  }
}
