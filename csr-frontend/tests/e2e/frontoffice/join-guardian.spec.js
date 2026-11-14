import { test, expect } from '@playwright/test';
import { gotoWithAuth } from '../helpers/auth.js';
import { routes } from '../helpers/routes.js';

test.describe('Join as guardian page', () => {
  test('button stays off until every field is filled', async ({ page }) => {
    await gotoWithAuth(page, routes.joinGuardian);
    const submit = page.getByRole('button', { name: 'เข้าร่วมด้วย LINE นี้' });
    await expect(submit).toBeDisabled();

    await page.getByLabel('รหัสประจำตัวนักเรียน').fill('12345');
    await expect(submit).toBeDisabled();
    await page.getByLabel('เบอร์โทรนักเรียน').fill('0812345678');
    await expect(submit).toBeDisabled();
    await page.getByLabel('คุณเป็น').selectOption('Mother');
    await expect(submit).toBeEnabled();
  });

  test('wrong details show the API error', async ({ page }) => {
    // The seeded mock user is already registered; act as a LINE user the seed data does not know.
    await page.route('**/api/register/join', (route) =>
      route.continue({
        headers: { ...route.request().headers(), authorization: `Bearer mock-token:e2e-ui-mother-${Date.now()}` }
      })
    );
    await gotoWithAuth(page, routes.joinGuardian);
    await page.getByLabel('รหัสประจำตัวนักเรียน').fill(`no-such-${Date.now()}`);
    await page.getByLabel('เบอร์โทรนักเรียน').fill('0812345678');
    await page.getByLabel('คุณเป็น').selectOption('Mother');
    await page.getByRole('button', { name: 'เข้าร่วมด้วย LINE นี้' }).click();

    await expect(page.getByRole('alert')).toContainText('ไม่พบข้อมูลที่ตรงกัน');
  });

  test('a LINE account that is already registered is told so', async ({ page }) => {
    await gotoWithAuth(page, routes.joinGuardian);
    await page.getByLabel('รหัสประจำตัวนักเรียน').fill('12345');
    await page.getByLabel('เบอร์โทรนักเรียน').fill('0812345678');
    await page.getByLabel('คุณเป็น').selectOption('Mother');
    await page.getByRole('button', { name: 'เข้าร่วมด้วย LINE นี้' }).click();

    await expect(page.getByRole('alert')).toContainText('ลงทะเบียนแล้ว');
  });

  test('register page links to it', async ({ page }) => {
    await gotoWithAuth(page, routes.register);
    await page.getByRole('link', { name: /เข้าร่วมด้วย LINE นี้/ }).click();
    await expect(page).toHaveURL(/\/register\/join$/);
  });
});
