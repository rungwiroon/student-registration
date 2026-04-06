import { test, expect } from '@playwright/test';
import { gotoWithAuth } from '../helpers/auth.js';
import { routes } from '../helpers/routes.js';

test.describe('Class List', () => {
  test('loads and shows classmates from seed data', async ({ page }) => {
    await gotoWithAuth(page, routes.classList);

    // Should not be stuck on loading
    await expect(page.getByText('กำลังเรียกดูรายชื่อ...')).toBeHidden();

    // Title
    await expect(page.getByRole('heading', { name: 'เพื่อนในห้อง' })).toBeVisible();

    // Student count badge — seed data has 3 students in ม.1/2
    await expect(page.getByText(/\d+ คน/)).toBeVisible();

    // At least one student card should be visible
    const studentCards = page.locator('.divide-y > div:not(:last-child)');
    const count = await studentCards.count();
    expect(count).toBeGreaterThanOrEqual(1);
  });
});

test.describe('Contacts', () => {
  test('loads and shows contact information', async ({ page }) => {
    await gotoWithAuth(page, routes.contacts);

    // Title
    await expect(page.getByRole('heading', { name: 'ติดต่อบุคลากร' })).toBeVisible();

    // Static contact: teacher
    await expect(page.getByText('อ.สมรักษ์ รักเรียน')).toBeVisible();

    // Static contact: parent network
    await expect(page.getByText('คุณรัตนาภรณ์')).toBeVisible();

    // Phone links should be present
    await expect(page.locator('a[href="tel:0891112222"]')).toBeVisible();
    await expect(page.locator('a[href="tel:0812223333"]')).toBeVisible();
  });
});
