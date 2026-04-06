import { test, expect } from '@playwright/test';
import { gotoWithAuth } from '../helpers/auth.js';
import { routes } from '../helpers/routes.js';

test.describe('Registration validation', () => {
  test('empty required fields show validation errors on submit', async ({ page }) => {
    await gotoWithAuth(page, routes.register);

    // Submit the empty form immediately
    await page.getByRole('button', { name: /ลงทะเบียน/ }).click();

    // Validation errors should appear for required fields
    // "กรุณากรอกรหัสประจำตัว" is unique (student only)
    await expect(page.getByText('กรุณากรอกรหัสประจำตัว')).toBeVisible();

    // "กรุณากรอกชื่อ" appears for both student and guardian — use .first()
    await expect(page.getByText('กรุณากรอกชื่อ').first()).toBeVisible();

    // "กรุณากรอกนามสกุล" also appears for both — use .first()
    await expect(page.getByText('กรุณากรอกนามสกุล').first()).toBeVisible();

    // Student number validation
    await expect(page.getByText(/กรุณากรอกเลขที่|เลขที่ต้องเป็นจำนวนเต็มบวก/)).toBeVisible();

    // Guardian required: relationType
    await expect(page.getByText('กรุณากรอกความสัมพันธ์')).toBeVisible();

    // Form should still be on the same page (not navigated away)
    await expect(page).toHaveURL(routes.register);
  });
});

test.describe('Registration submit', () => {
  test('valid form submits successfully and redirects to dashboard', async ({ page }) => {
    await gotoWithAuth(page, routes.register);

    // Fill student section — use unique Student ID to avoid collision with seed data
    const uniqueStudentId = `99${Date.now().toString().slice(-3)}`;
    await page.getByPlaceholder('เช่น 30558').fill(uniqueStudentId);
    await page.getByPlaceholder('เด็กชาย...').fill('ทดสอบ');
    await page.getByPlaceholder('นามสกุล').first().fill('สมมติ');
    await page.getByPlaceholder('1').fill('5');

    // Fill guardian 1 section (emerald-colored)
    const guardian1 = page.locator('section.bg-emerald-50');
    await guardian1.getByPlaceholder('นาย...').fill('นายทดสอบ');
    await guardian1.getByPlaceholder('นามสกุล').first().fill('ผู้ปกครอง');
    await guardian1.locator('select').first().selectOption('Father');
    await guardian1.getByPlaceholder('08...').first().fill('0812345678');

    // Set up dialog handler BEFORE clicking submit
    page.on('dialog', (dialog) => {
      expect(dialog.type()).toBe('alert');
      dialog.accept();
    });

    // Submit
    await page.getByRole('button', { name: /ลงทะเบียน/ }).click();

    // Should redirect to dashboard (or home page)
    await expect(page).toHaveURL(new RegExp(`${routes.dashboard}|/`), { timeout: 15_000 });
  });
});
