import { test, expect } from '../../Frontend/node_modules/@playwright/test';
test('theme persistence, keyboard entry and responsive visual review', async ({ page }) => {
  await page.route('**/health', route => route.fulfill({ contentType: 'application/json', body: '{"status":"healthy"}' }));
  await page.emulateMedia({ colorScheme: 'light', reducedMotion: 'reduce' });
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.goto('/');
  await expect(page.getByRole('status')).toHaveText('API is reachable');
  await page.keyboard.press('Tab');
  await expect(page.getByRole('link', { name: 'Skip to main content' })).toBeFocused();
  await page.keyboard.press('Tab');
  await page.screenshot({ path: '../document/screenshots/baseline-light.png', fullPage: true });
  await page.getByRole('button', { name: 'Switch color theme' }).click();
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
  await page.reload();
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
  await expect(page.getByRole('status')).toHaveText('API is reachable');
  await page.screenshot({ path: '../document/screenshots/baseline-dark.png', fullPage: true });
  await page.setViewportSize({ width: 390, height: 844 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: '../document/screenshots/baseline-mobile.png', fullPage: true });
});

