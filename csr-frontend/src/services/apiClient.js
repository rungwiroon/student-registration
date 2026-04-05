async function createApiError(response) {
  const message = (await response.text()) || `Request failed with status ${response.status}`;
  const error = new Error(message);
  error.status = response.status;
  return error;
}

export async function apiFetch(path, token, options = {}) {
  const headers = new Headers(options.headers || {});

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(path, {
    ...options,
    headers
  });

  return response;
}

export async function apiJson(path, token, options = {}) {
  const response = await apiFetch(path, token, {
    ...options,
    headers: {
      Accept: 'application/json',
      ...(options.headers || {})
    }
  });

  if (!response.ok) {
    throw await createApiError(response);
  }

  const text = await response.text();
  return text ? JSON.parse(text) : null;
}

export async function apiBlob(path, token, options = {}) {
  const response = await apiFetch(path, token, options);

  if (!response.ok) {
    throw await createApiError(response);
  }

  return response.blob();
}
