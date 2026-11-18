import { test, expect } from '@playwright/test';
import { gotoWithAuth } from '../helpers/auth.js';

test.describe('Backoffice vote results: per-family list', () => {
  test('lists families and the filters narrow the list', async ({ page }) => {
    await gotoWithAuth(page, '/backoffice/venue-vote');

    await expect(page.getByRole('heading', { name: 'รายครอบครัว' })).toBeVisible();
    const summary = page.getByTestId('family-summary');
    await expect(summary).toContainText('ครอบครัว');

    const count = async () => Number((await summary.textContent()).match(/แสดง (\d+) ครอบครัว/)[1]);
    const all = await count();
    expect(all).toBeGreaterThan(0);

    await page.getByRole('button', { name: 'ยังไม่โหวต', exact: true }).click();
    const notVoted = await count();
    await page.getByRole('button', { name: 'โหวตแล้ว', exact: true }).click();
    const voted = await count();
    expect(notVoted + voted).toBe(all);

    await page.getByRole('button', { name: 'ทั้งหมด', exact: true }).click();
    await page.getByLabel('ค้นหาครอบครัว').fill('zzz-no-such-family');
    await expect(page.getByText('ไม่พบครอบครัวตามเงื่อนไข')).toBeVisible();
  });
});
