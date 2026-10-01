import { test, expect } from '../../Frontend/node_modules/@playwright/test';
test('unknown route has a usable recovery path', async ({ page }) => {
  await page.route('**/health', route => route.fulfill({ contentType: 'application/json', body: '{"status":"healthy"}' }));
  await page.goto('/missing');
  await expect(page.getByRole('heading', { name: 'This page could not be found' })).toBeVisible();
  await page.getByRole('link', { name: 'Return to overview' }).click();
  await expect(page.getByRole('heading', { name: 'Your notification control room' })).toBeVisible();
});
