import { apiBlob, apiFetch, apiJson } from './apiClient';

function normalizeOptionalText(value) {
  return value ? value : '';
}

function toPayload(form) {
  return {
    student: {
      studentId: form.student.studentId || null,
      name: form.student.name,
      oldRoom: form.student.oldRoom || null,
      oldNo: form.student.oldNo || null,
      newRoom: form.student.newRoom || null,
      newNo: form.student.newNo || null,
      phone: normalizeOptionalText(form.student.phone),
      bloodType: normalizeOptionalText(form.student.bloodType),
      dob: normalizeOptionalText(form.student.dob)
    },
    guardian: {
      relationType: form.guardian.relationType,
      name: form.guardian.name,
      phone: form.guardian.phone,
      occupation: normalizeOptionalText(form.guardian.occupation),
      email: normalizeOptionalText(form.guardian.email)
    }
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

  if (files.guardianPhoto) {
    formData.append('guardianPhoto', files.guardianPhoto);
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
