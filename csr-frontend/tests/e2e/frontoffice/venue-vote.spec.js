import { test, expect } from '@playwright/test';
import { gotoWithAuth } from '../helpers/auth.js';
import { routes } from '../helpers/routes.js';

test.describe('Venue Vote page', () => {
  test('shows the four venues with detail links', async ({ page }) => {
    await gotoWithAuth(page, routes.venueVote);

    await expect(page.getByRole('radio')).toHaveCount(4);
    await expect(page.getByText('The Big Tree Café')).toBeVisible();
    await expect(page.getByText('สวนลุงหมง')).toBeVisible();
    await expect(page.getByRole('link', { name: 'ดูรายละเอียดสถานที่' })).toHaveCount(4);
  });

  test('vote cannot be sent until a valid attendee count is entered', async ({ page }) => {
    await gotoWithAuth(page, routes.venueVote);

    const radios = page.getByRole('radio');
    await expect(radios).toHaveCount(4);
    // Pick a venue that is not the current vote so a venue change is always pending.
    const target = (await radios.nth(0).isChecked()) ? radios.nth(1) : radios.nth(0);
    await target.check();

    const count = page.getByLabel('จำนวนคนที่จะไป (รวมตัวเอง)');
    const submit = page.getByRole('button', { name: /ยืนยันโหวต|เปลี่ยนโหวต/ });

    await count.fill('');
    await expect(submit).toBeDisabled();

    await count.fill('0');
    await expect(page.getByRole('alert')).toBeVisible();
    await expect(submit).toBeDisabled();

    await count.fill('11');
    await expect(submit).toBeDisabled();

    await count.fill('3');
    await expect(page.getByRole('alert')).toBeHidden();
    await expect(submit).toBeEnabled();
  });

  test('voting shows the tally with people and marks my choice', async ({ page }) => {
    await gotoWithAuth(page, routes.venueVote);

    const radios = page.getByRole('radio');
    await expect(radios).toHaveCount(4);
    const target = (await radios.nth(0).isChecked()) ? radios.nth(1) : radios.nth(0);
    await target.check();
    await page.getByLabel('จำนวนคนที่จะไป (รวมตัวเอง)').fill('4');

    await page.getByRole('button', { name: /ยืนยันโหวต|เปลี่ยนโหวต/ }).click();

    await expect(page.getByRole('status')).toHaveText('บันทึกโหวตแล้ว');
    await expect(page.getByText('โหวตของคุณ')).toHaveCount(1);
    await expect(page.getByText(/โหวตแล้วทั้งหมด \d+ ครอบครัว · รวม \d+ คน/)).toBeVisible();
    await expect(page.getByText(/\d+ โหวต · \d+ คน/)).toHaveCount(4);
    await expect(target).toBeChecked();
    await expect(page.getByLabel('จำนวนคนที่จะไป (รวมตัวเอง)')).toHaveValue('4');
  });

  test('stepper buttons change the count within the limits', async ({ page }) => {
    await gotoWithAuth(page, routes.venueVote);

    const count = page.getByLabel('จำนวนคนที่จะไป (รวมตัวเอง)');
    await count.fill('1');
    await expect(page.getByRole('button', { name: 'ลดจำนวนคน' })).toBeDisabled();

    await page.getByRole('button', { name: 'เพิ่มจำนวนคน' }).click();
    await expect(count).toHaveValue('2');

    await count.fill('10');
    await expect(page.getByRole('button', { name: 'เพิ่มจำนวนคน' })).toBeDisabled();
  });

  test('parent without a registered student sees a register prompt, not an error', async ({ page }) => {
    // Same page, but the API sees an unregistered LINE user (see mock-token:<id> in LiffAuthMiddleware).
    await page.route('**/api/venue-vote', (route) =>
      route.continue({
        headers: { ...route.request().headers(), authorization: 'Bearer mock-token:e2e-unregistered-parent' }
      })
    );

    await gotoWithAuth(page, routes.venueVote);

    await expect(page.getByText('ยังไม่ได้ลงทะเบียนนักเรียน')).toBeVisible();
    await expect(page.getByText('โหลดข้อมูลไม่สำเร็จ')).toBeHidden();
    await expect(page.getByRole('link', { name: 'ลงทะเบียนนักเรียน' })).toBeVisible();
    await expect(page.getByRole('radio')).toHaveCount(4);
    await expect(page.getByRole('radio').first()).toBeDisabled();
    await expect(page.getByRole('button', { name: /ยืนยันโหวต|เปลี่ยนโหวต/ })).toHaveCount(0);
  });

  test('dashboard links to the vote page', async ({ page }) => {
    await gotoWithAuth(page, routes.dashboard);
    await page.getByRole('link', { name: /โหวตสถานที่จัดกิจกรรม/ }).click();
    await expect(page).toHaveURL(/\/venue-vote$/);
  });

  test('a redirect query on the app root opens the vote page', async ({ page }) => {
    await gotoWithAuth(page, `/?redirect=${encodeURIComponent(routes.venueVote)}`);
    await expect(page).toHaveURL(/\/venue-vote$/);
  });
});
