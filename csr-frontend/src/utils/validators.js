/**
 * Validation helpers for registration form fields.
 * Pure functions — easy to unit test.
 */

const PHONE_PATTERN = /^0[0-9]{8,9}$/;
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function required(value, label) {
  if (!value || (typeof value === 'string' && !value.trim())) {
    return `กรุณากรอก${label}`;
  }
  return '';
}

export function phone(value) {
  if (!value) return '';
  const digits = value.replace(/[-\s]/g, '');
  if (!PHONE_PATTERN.test(digits)) {
    return 'รูปแบบเบอร์โทรไม่ถูกต้อง (เช่น 0812345678)';
  }
  return '';
}

export function email(value) {
  if (!value) return '';
  if (!EMAIL_PATTERN.test(value)) {
    return 'รูปแบบอีเมลไม่ถูกต้อง';
  }
  return '';
}

export function positiveInteger(value, label, max = Infinity) {
  if (value === null || value === undefined || value === '') {
    return `กรุณากรอก${label}`;
  }
  if (!Number.isInteger(value) || value <= 0) {
    return `${label}ต้องเป็นจำนวนเต็มบวก`;
  }
  if (value > max) {
    return `${label}ต้องไม่เกิน ${max}`;
  }
  return '';
}

/**
 * Date-of-birth format helpers.
 * UI shows dd/MM/yyyy, API expects yyyy-MM-dd.
 */
export function parseDobToApi(displayValue) {
  if (!displayValue) return '';
  if (displayValue instanceof Date) {
    return formatDateToApi(displayValue);
  }
  const m = String(displayValue).match(/^(\d{2})\/(\d{2})\/(\d{4})$/);
  if (!m) return String(displayValue);
  return `${m[3]}-${m[2]}-${m[1]}`;
}

export function parseApiToDob(apiValue) {
  if (!apiValue) return null;
  // Already in yyyy-MM-dd format — return as-is for VueDatePicker with model-type
  const s = String(apiValue);
  if (/^\d{4}-\d{2}-\d{2}$/.test(s)) return s;
  return null;
}

function formatDateToApi(date) {
  const y = date.getFullYear();
  const mo = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${mo}-${d}`;
}
