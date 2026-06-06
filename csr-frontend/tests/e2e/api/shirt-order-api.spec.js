import { test, expect } from '@playwright/test';

/**
 * API Integration tests for /api/v1/school-shirt/order
 * These tests verify the backend endpoint contract directly.
 */

test.describe('Shirt Order API', () => {
  const apiUrl = '/api/v1/school-shirt/order';

  test('returns 401 without auth token', async ({ request }) => {
    const formData = new FormData();
    formData.append('payload', JSON.stringify({
      studentName: 'Test',
      studentNumber: '1',
      items: [{ design: 'A', size: 'M', quantity: 1, unitPrice: 250 }],
      totalAmount: 250
    }));

    const response = await request.post(apiUrl, {
      multipart: {
        payload: JSON.stringify({
          studentName: 'Test',
          studentNumber: '1',
          items: [{ design: 'A', size: 'M', quantity: 1, unitPrice: 250 }],
          totalAmount: 250
        })
      }
    });

    expect(response.status()).toBe(401);
  });

  test('returns 400 with missing payload', async ({ request }) => {
    // Send multipart without payload field
    const response = await request.post(apiUrl, {
      headers: {
        'Authorization': 'Bearer mock-token'
      },
      multipart: {
        proofOfPayment: {
          name: 'slip.png',
          mimeType: 'image/png',
          buffer: Buffer.from('fake')
        }
      }
    });

    expect(response.status()).toBe(400);
  });

  test('returns 400 with missing slip file', async ({ request }) => {
    const response = await request.post(apiUrl, {
      headers: {
        'Authorization': 'Bearer mock-token'
      },
      multipart: {
        payload: JSON.stringify({
          studentName: 'Test',
          studentNumber: '1',
          items: [{ design: 'A', size: 'M', quantity: 1, unitPrice: 250 }],
          totalAmount: 250
        })
      }
    });

    expect(response.status()).toBe(400);
  });
});
