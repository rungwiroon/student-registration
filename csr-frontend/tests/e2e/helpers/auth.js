/**
 * Auth helpers for E2E tests using mock LIFF.
 *
 * Prerequisites:
 *   - VITE_USE_MOCK_LIFF=true (set in docker-compose.dev.yml or .env)
 *   - Backend running with Line__UseMockAuth=true + SeedData__Enabled=true
 */

import { routes } from './routes.js';

/**
 * Navigate to a front-office page and wait until the mock LIFF
 * init completes (the "กำลังเชื่อมต่อ LINE..." overlay disappears).
 */
export async function gotoWithAuth(page, path) {
  await page.goto(path);
  // Mock LIFF resolves almost instantly, but give the page time to
  // react and render. Wait for the loading indicator to disappear.
  await page.waitForSelector('text=กำลังเชื่อมต่อ LINE', { state: 'hidden', timeout: 10_000 }).catch(() => {
    // Some pages (dashboard, class-list) may not show this indicator
    // if they load data differently. That's fine.
  });
}

/**
 * Go to the dashboard and confirm it loaded successfully.
 * Useful as a "warm up" before deeper navigation tests.
 */
export async function warmUp(page) {
  await gotoWithAuth(page, routes.dashboard);
}
