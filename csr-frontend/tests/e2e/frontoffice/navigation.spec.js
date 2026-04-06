import { test, expect } from '@playwright/test';
import { gotoWithAuth } from '../helpers/auth.js';
import { routes } from '../helpers/routes.js';

test.describe('Navigation', () => {
  test('document page loads and home button navigates to dashboard', async ({ page }) => {
    await gotoWithAuth(page, routes.document);

    // Wait for data to load
    await expect(page.getByText('กำลังโหลดข้อมูล...')).toBeHidden();

    // Document title should be visible
    await expect(page.getByRole('heading', { name: 'ใบแนะนำตัวนักเรียนและผู้ปกครอง' })).toBeVisible();

    // Seed student data should appear
    await expect(page.getByText('30558')).toBeVisible();
    await expect(page.getByText('บีม')).toBeVisible();

    // Click "หน้าแรก" (Home) — should go to dashboard
    await page.getByRole('button', { name: /หน้าแรก/ }).click();
    await expect(page).toHaveURL(routes.dashboard);
  });

  test('edit profile page shows form and navigates home', async ({ page }) => {
    await gotoWithAuth(page, routes.editProfile);

    // Wait for LIFF init + data load
    await page.waitForSelector('text=กำลังเชื่อมต่อ LINE', { state: 'hidden', timeout: 10_000 }).catch(() => {});
    await page.waitForSelector('text=กำลังโหลดข้อมูลเดิม', { state: 'hidden', timeout: 10_000 }).catch(() => {});

    // Form title for edit mode
    await expect(page.getByText('แก้ไขข้อมูลผู้ปกครองและนักเรียน')).toBeVisible();

    // Click "หน้าแรก" button
    await page.getByRole('button', { name: /หน้าแรก/ }).click();
    await expect(page).toHaveURL(routes.dashboard);
  });

  test('edit profile back button returns to dashboard', async ({ page }) => {
    // Start at dashboard, navigate to edit, then go back
    await gotoWithAuth(page, routes.dashboard);
    await expect(page.getByText('ข้อมูลล่าสุด')).toBeVisible();

    // Navigate to edit profile via the button
    await page.getByRole('link', { name: /แก้ไขข้อมูล/ }).click();
    await expect(page).toHaveURL(routes.editProfile);

    // Click "กลับ" (Back) button — should return to dashboard
    await page.getByRole('button', { name: /กลับ/ }).click();
    await expect(page).toHaveURL(routes.dashboard);
  });
});
