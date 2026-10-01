import { test, expect } from '../../Frontend/node_modules/@playwright/test';
test('responsive baseline, failure recovery and theme control', async ({ page }) => {
  await page.route('**/health', route => route.fulfill({ status: 503, contentType: 'application/json', body: '{}' }));
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Your notification control room' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Retry connection' })).toBeVisible();
  await page.unroute('**/health');
  await page.route('**/health', route => route.fulfill({ contentType: 'application/json', body: '{"status":"healthy"}' }));
  await page.getByRole('button', { name: 'Retry connection' }).click();
  await expect(page.getByRole('status')).toHaveText('API is reachable');
  await page.getByRole('button', { name: 'Switch color theme' }).click();
  await page.setViewportSize({ width: 390, height: 844 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});
