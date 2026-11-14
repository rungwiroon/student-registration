import { test, expect } from '@playwright/test';
import { randomBytes } from 'node:crypto';
import { gotoWithAuth } from '../helpers/auth.js';

/** Backoffice staff page: fix a wrongly entered LINE id (mock auth: default user is a Teacher). */

const newLineId = () => `U${randomBytes(16).toString('hex')}`;
const teacher = { Authorization: 'Bearer mock-token' };

async function seedStaff(request, name) {
  const lineUserId = newLineId();
  const response = await request.post('/api/backoffice/staff', {
    headers: teacher,
    data: { lineUserId, name, role: 'ParentNetworkStaff', position: 'ประธาน' }
  });
  expect(response.status()).toBe(200);
  return { ...(await response.json()), lineUserId };
}

// The page renders both a table (desktop) and cards (mobile); only one is visible.
const visible = (locator) => locator.locator('visible=true');

test.describe('Backoffice staff: change LINE id', () => {
  test('typing the correct id moves the row, and the list shows the new id', async ({ page, request }) => {
    const name = `ปทุมพร ${Date.now()}`;
    const staff = await seedStaff(request, name);
    const correctId = newLineId();

    await gotoWithAuth(page, '/backoffice/staff');
    const row = visible(page.locator('tr, div.p-4').filter({ hasText: name })).first();
    await visible(row.getByText('แก้ไข', { exact: true })).first().click();

    const idInput = visible(page.getByLabel('LINE User ID', { exact: true })).first();
    await expect(idInput).toHaveValue(staff.lineUserId);
    await idInput.fill(correctId);

    page.once('dialog', (d) => d.accept());
    await visible(page.getByText('บันทึก', { exact: true })).first().click();

    await expect(visible(page.getByText(correctId)).first()).toBeVisible();
    await expect(page.getByText(staff.lineUserId)).toHaveCount(0);
  });

  test('picking a registered guardian fills the id; a refused change shows the API reason', async ({ page, request }) => {
    const name = `เครือข่าย ${Date.now()}`;
    await seedStaff(request, name);

    await gotoWithAuth(page, '/backoffice/staff');
    const row = visible(page.locator('tr, div.p-4').filter({ hasText: name })).first();
    await visible(row.getByText('แก้ไข', { exact: true })).first().click();

    // Seeded guardian "ผู้ปกครอง ทดสอบ" (LINE id mock-line-user-id).
    await visible(page.getByLabel('ค้นหาผู้ใช้ LINE')).first().fill('ทดสอบ');
    await page.getByRole('option').first().getByRole('button').click();
    await expect(visible(page.getByLabel('LINE User ID', { exact: true })).first()).toHaveValue('mock-line-user-id');

    // First dialog is the "change LINE?" confirm (accept), the second is the failure alert.
    const messages = [];
    page.on('dialog', async (d) => { messages.push(d.message()); await d.accept(); });
    await visible(page.getByText('บันทึก', { exact: true })).first().click();

    await expect.poll(() => messages.length).toBe(2);
    expect(messages[0]).toContain('ผู้ปกครอง ทดสอบ');
    // The seeded mock id is not a real U… id, so the API refuses it; the page must show that reason.
    expect(messages[1]).toContain('LINE ID ไม่ถูกต้อง');
  });
});
