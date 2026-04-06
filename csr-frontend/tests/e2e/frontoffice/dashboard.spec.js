import { test, expect } from '@playwright/test';
import { gotoWithAuth } from '../helpers/auth.js';
import { routes } from '../helpers/routes.js';

test.describe('Dashboard', () => {
  test('loads and shows student profile from seed data', async ({ page }) => {
    await gotoWithAuth(page, routes.dashboard);

    // Should NOT be stuck on loading
    await expect(page.getByText('กำลังตรวจสอบข้อมูล...')).toBeHidden();

    // Hero card should render with student data from seed
    const heroCard = page.locator('section.bg-gradient-to-br');
    await expect(heroCard).toBeVisible();

    // Student ID from seed data: 30558
    await expect(page.getByText('30558')).toBeVisible();

    // "ข้อมูลล่าสุด" section should be visible
    await expect(page.getByText('ข้อมูลล่าสุด')).toBeVisible();

    // Edit profile button should be visible
    await expect(page.getByRole('link', { name: /แก้ไขข้อมูล/ })).toBeVisible();
  });
});
