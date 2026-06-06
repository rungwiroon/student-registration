import { apiFetch } from './apiClient';

export async function submitShirtOrder(token, form, slipFile, totalAmount, orderSummary) {
  const formData = new FormData();

  const payload = {
    lineDisplayName: form.lineDisplayName || '',
    studentName: form.studentName || '',
    studentNumber: form.studentNumber || '',
    items: form.items
      .filter(i => i.quantity > 0)
      .map(i => ({
        design: i.design,
        size: i.size,
        quantity: i.quantity,
        unitPrice: i.unitPrice
      })),
    totalAmount
  };

  formData.append('payload', JSON.stringify(payload));

  if (slipFile) {
    formData.append('proofOfPayment', slipFile);
  }

  const response = await apiFetch('/api/v1/school-shirt/order', token, {
    method: 'POST',
    body: formData
  });

  if (!response.ok) {
    const message = (await response.text()) || 'สั่งซื้อไม่สำเร็จ';
    const error = new Error(message);
    error.status = response.status;
    throw error;
  }

  return response.json();
}
